using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.ComponentModel;

namespace BeatDancer
{
    class BeatManager : INotifyPropertyChanged
    {
        // ビート検出に使うサンプルの長さ(秒)
        private double _secLen = 5;

        // ビート検出に使うサンプルの長さ (サンプル数, _secLen から算出)
        private long _sampleBufLen = 0;

        // Beat Detection を発火するのに最低限必要な長さ
        private long _minSampleBufLen = 1000;

        // キャプチャ回数
        private int _captureCnt = 1;
        // Beat Detection インターバル(キャプチャ回数)
        private const int _detectionInterval = 10;

        // キャプチャオプション
        private CaptureOption _captureOpt = null;
        private DetectStatus _detectOpt = null;

        // 踊り手Managerへのポインタ
        private DancerManager _dancerManager = null;
        public DancerManager DancerManager { get { return _dancerManager; } set { _dancerManager = value; } }

        // データ
        private List<double[]> _samples = new List<double[]>();
        private List<BeatStatus> _results = new List<BeatStatus>();
        private BeatStatus _result = null;
        private int _resultMax = 5;

        // 結果
        public double Bpm {
            get {
                if (_result == null || _result.BpmDancer <= 0 || _result.BeatTickSmooth <= 0)
                {
                    return 0;
                }
                lock (_result)
                {
                    return _result.BpmDancer;
                }
            }
        }
        public double[] BpmEnergies
        {
            get
            {
                if (_detectOpt == null)
                {
                    return new double[0];
                }
                lock (_detectOpt)
                {
                    double[] tmp = new double[_detectOpt.BpmEnergies.Length];
                    Array.Copy(_detectOpt.BpmEnergies, tmp, tmp.Length);
                    return tmp;
                }
            }
        }
        public double BeatRatio
        {
            get
            {
                if (_result != null && _result.BpmDancer > 0 && _result.BeatTickSmooth > 0)
                {
                    lock (_result)
                    {
                        long curTick = Environment.TickCount;
                        double r = _result.BpmDancer * (curTick - _result.BeatTickSmooth) / 60000.0;
                        r = r - Math.Truncate(r);
                        if (r < 0) r += 1.0;
                        return r;
                    }
                }
                else
                {
                    return 0.0;
                }
            }
        }

        // 初期化
        public BeatManager(CaptureOption opt)
        {
            _captureOpt = opt;
            _detectOpt = new DetectStatus();
            _detectOpt.SamplePerSec = _captureOpt.SamplePerSec;
            _sampleBufLen = (long)Math.Ceiling(_secLen * _captureOpt.SamplePerSec);
            _minSampleBufLen = (long)(_sampleBufLen / 2.0);
        }

        // サンプリング->発火機構
        public void AddSample(long sampleStartTick, double[] sample)
        {
            double[] s = new double[sample.Length];
            Array.Copy(sample, s, sample.Length);
            _samples.Add(s);

            if (/* cond */ _captureCnt == 0)
            {
                // ビート検出用の配列を作成する
                double[] buffer = new double[_sampleBufLen];
                long copiedSize = 0;
                int i = _samples.Count - 1;
                while (i >= 0)
                {
                    long copySize = _samples[i].Length;
                    if (copySize + copiedSize > _sampleBufLen) copySize = _sampleBufLen - copiedSize;
                    Array.Copy(_samples[i], 0, buffer, _sampleBufLen - copiedSize - copySize, copySize);

                    copiedSize += copySize;
                    i -= 1;
                    if (copiedSize >= _sampleBufLen) break;
                }

                if (copiedSize >= _minSampleBufLen)
                {
                    double[] tmp = new double[copiedSize];
                    if (copiedSize < _sampleBufLen)
                    {
                        Array.Copy(buffer, buffer.Length - copiedSize, tmp, 0, copiedSize);
                        buffer = tmp;
                    }

                    // 検出結果を入れるobjを用意
                    BeatStatus bs = new BeatStatus();
                    bs.SampleStartTick = sampleStartTick;

                    // ビート検出を別スレッドで実行
                    var task = Task.Factory.StartNew(() =>
                    {
                        Detect(buffer, ref bs);
                    });

                    // 不要な分を_samplesから落とす
                    if (i >= 0)
                    {
                        _samples.RemoveRange(0, i + 1);
                    }
                }
            }
            _captureCnt = (_captureCnt + 1) % _detectionInterval;
        }

        // 検出処理
        public void Detect(double[] buffer, ref BeatStatus st)
        {
            // BpmRawを算出する
            BeatDetector detector = new BeatDetector();
            List<double[]> features = detector.DetectBpm(buffer, ref _detectOpt, ref st);

            if (st.BpmRaw > 0)
            {
                Console.Error.WriteLine("beat detect success");

                // Dancerに合わせてBpmを調整する
                st.BpmDancer = st.BpmRaw;
                if (_dancerManager != null)
                {
                    Dancer dancer = _dancerManager.Dancer;
                    double minBpm = dancer.MinBpm;
                    double maxBpm = dancer.MaxBpm;
                    if (st.BpmDancer > maxBpm)
                    {
                        while (st.BpmDancer > maxBpm)
                            st.BpmDancer /= 2.0;
                    }
                    if (st.BpmDancer < minBpm)
                    {
                        while (st.BpmDancer < minBpm)
                            st.BpmDancer *= 2.0;
                    }
                }

                // Beatを探す
                detector.AdjustBeat(features, ref _detectOpt, ref st);

                if (_results.Count > _resultMax)
                {
                    for (int i = 0; i < _resultMax - _results.Count; i++)
                    {
                        _results.RemoveAt(0);
                    }
                }
                _results.Add(st);

                // Beat start tick を smoothing する
                double avg = -1;
                for (int i = 0; i < _results.Count - 1; i++)
                {
                    BeatStatus s = _results[i];
                    double tmp = (st.BeatTickRaw - s.BeatTickSmooth) * s.BpmDancer / 60000.0;
                    double a = (long)(tmp >= 0.5 ? Math.Ceiling(tmp) : Math.Floor(tmp)) * 60000 / (long)s.BpmDancer + (long)s.BeatTickSmooth;

                    if (avg < 0)
                    {
                        avg = a;
                    }
                    else
                    {
                        avg = avg * 0.5 + a * 0.5;
                    }
                }
                st.BeatTickSmooth = avg * 0.9 + st.BeatTickRaw * 0.1;

                if (_result == null) _result = new BeatStatus();
                lock (_result)
                {
                    _result = st;
                    OnPropertyChanged("Bpm");
                    OnPropertyChanged("BpmEnergies");
                    OnPropertyChanged("BeatRatio");
                }
            }
            else
            {
                Console.Error.WriteLine("beat detect failed");
            }
        }


        public event PropertyChangedEventHandler PropertyChanged;
        protected void OnPropertyChanged(string name)
        {
            if (PropertyChanged != null)
            {
                PropertyChanged(this, new PropertyChangedEventArgs(name));
            }
        }
    }
}
