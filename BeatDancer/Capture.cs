using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Runtime.InteropServices;
using DirectShowLib;

namespace BeatDancer
{
    class Capture : IDisposable
    {
        /// <summary>00000001-0000-0010-8000-00AA00389B71 MEDIASUBTYPE_PCM</summary>
        public static readonly Guid PCM = new Guid(0x00000001, 0x0000, 0x0010, 0x80, 0x00, 0x00, 0xAA, 0x00, 0x38, 0x9B, 0x71);
        /// <summary>e436eb8b-524f-11ce-9f53-0020af0ba770 MEDIASUBTYPE_WAVE</summary>
        public static readonly Guid Wave = new Guid(0xe436eb8b, 0x524f, 0x11ce, 0x9f, 0x53, 0x00, 0x20, 0xaf, 0x0b, 0xa7, 0x70);

        /// <summary>フィルタグラフマネージャ</summary>
        private IFilterGraph2 graphBuilder;
        /// <summary>キャプチャグラフビルダ</summary>
        private ICaptureGraphBuilder2 captureGraphBuilder;
        /// <summary>ソースフィルタ</summary>
        private IBaseFilter captureFilter;
        /// <summary>サンプルグラバ</summary>
        private ISampleGrabber sampleGrabber;

        private DSAudioSampler _sampler = null;
        public DSAudioSampler Sampler
        {
            get { return _sampler; }
        }

        public Capture()
        {

        }

        /// <summary>
        /// ComObjectの解放
        /// </summary>
        public void Dispose()
        {
            IMediaControl ctrl = graphBuilder as IMediaControl;
            ctrl.Stop();

            /// 解放
            if (sampleGrabber != null)
            {
                Marshal.ReleaseComObject(sampleGrabber);
                sampleGrabber = null;
            }
            if (captureGraphBuilder != null)
            {
                Marshal.ReleaseComObject(captureGraphBuilder);
                captureGraphBuilder = null;
            }
            if (captureFilter != null)
            {
                Marshal.ReleaseComObject(captureFilter);
                captureFilter = null;
            }
            if (graphBuilder != null)
            {
                Marshal.ReleaseComObject(graphBuilder);
                graphBuilder = null;
            }
        }

        private DsDevice[] _devices = null;
        public DsDevice[] Devices
        {
            get
            {
                if (_devices == null)
                {
                    // 接続されているデバイスリスト取得
                    _devices = DsDevice.GetDevicesOfCat(FilterCategory.AudioInputDevice);
                    DsDevice[] test = DsDevice.GetDevicesOfCat(FilterCategory.AudioRendererCategory);
                    test = DsDevice.GetDevicesOfCat(FilterCategory.KSAudioDevice);
                }
                return _devices;
            }
        }

        private DsDevice _capDevice = null;
        public DsDevice CapDevice
        {
            get { return _capDevice; }
            set { _capDevice = value; }
        }

        public void CreateGraph()
        {
            try
            {
                int result = 0;

                // フィルタグラフマネージャ作成
                graphBuilder = new FilterGraph() as IFilterGraph2;

                // キャプチャグラフビルダ作成
                captureGraphBuilder = new CaptureGraphBuilder2() as ICaptureGraphBuilder2;

                //captureGraphBuilder（キャプチャグラフビルダ）をgraphBuilder（フィルタグラフマネージャ）に追加．
                result = captureGraphBuilder.SetFiltergraph(graphBuilder);
                DsError.ThrowExceptionForHR(result);

                // ソースフィルタ作成
                // キャプチャデバイスをソースフィルタに対応付ける
                captureFilter = null;
                result = graphBuilder.AddSourceFilterForMoniker(
                    _capDevice.Mon, null, _capDevice.Name, out captureFilter);
                DsError.ThrowExceptionForHR(result);

                // サンプルグラバ作成
                sampleGrabber = new SampleGrabber() as ISampleGrabber;

                // フィルタと関連付ける
                IBaseFilter grabFilter = sampleGrabber as IBaseFilter;

                // キャプチャするオーディオのフォーマットを設定
                AMMediaType amMediaType = new AMMediaType();
                amMediaType.majorType = MediaType.Audio;
                amMediaType.subType = MediaSubType.PCM;
                amMediaType.formatPtr = IntPtr.Zero;
                result = sampleGrabber.SetMediaType(amMediaType);
                DsError.ThrowExceptionForHR(result);
                DsUtils.FreeAMMediaType(amMediaType);

                // callback 登録
                sampleGrabber.SetOneShot(false);
                DsError.ThrowExceptionForHR(result);

                result = sampleGrabber.SetBufferSamples(true);
                DsError.ThrowExceptionForHR(result);

                // キャプチャするフォーマットを取得
                object o;
                result = captureGraphBuilder.FindInterface(
                    DsGuid.FromGuid(PinCategory.Capture),
                    DsGuid.FromGuid(MediaType.Audio),
                    captureFilter,
                    typeof(IAMStreamConfig).GUID, out o);
                DsError.ThrowExceptionForHR(result);
                IAMStreamConfig config = o as IAMStreamConfig;
                AMMediaType media;
                result = config.GetFormat(out media);
                DsError.ThrowExceptionForHR(result);

                WaveFormatEx wf = new WaveFormatEx();
                Marshal.PtrToStructure(media.formatPtr, wf);

                CaptureOption opt = new CaptureOption(wf);
                _sampler = new DSAudioSampler(opt);

                DsUtils.FreeAMMediaType(media);
                Marshal.ReleaseComObject(config);

                result = sampleGrabber.SetCallback(_sampler, 1);
                DsError.ThrowExceptionForHR(result);

                //grabFilter(変換フィルタ)をgraphBuilder（フィルタグラフマネージャ）に追加．
                result = graphBuilder.AddFilter(grabFilter, "Audio Grab Filter");
                DsError.ThrowExceptionForHR(result);


                //キャプチャフィルタをサンプルグラバーフィルタに接続する
                result = captureGraphBuilder.RenderStream(
                    DsGuid.FromGuid(PinCategory.Capture),
                    DsGuid.FromGuid(MediaType.Audio),
                    captureFilter, null, grabFilter);
                DsError.ThrowExceptionForHR(result);
            }
            catch (Exception ex)
            {
                System.Windows.MessageBox.Show(ex.Message);
            }
        }

        public void StartCapture()
        {
            try
            {
                IMediaControl ctrl = graphBuilder as IMediaControl;
                ctrl.Run();
            }
            catch (Exception ex)
            {
                System.Windows.MessageBox.Show(ex.Message);
            }
        }
    }

    class CaptureOption
    {
        private int _channelNum = 0;
        public int ChannelNum { get { return _channelNum; } }

        private double _samplePerSec = 0;
        public double SamplePerSec { get { return _samplePerSec; } set { _samplePerSec = value; } }

        public CaptureOption(WaveFormatEx wf)
        {
            _channelNum = wf.nChannels;
            _samplePerSec = wf.nSamplesPerSec;
        }
    }

    class DSAudioSampler : ISampleGrabberCB
    {
        private int _sampleStep = 16;
        private CaptureOption _captureOpt = null;

        private List<BeatManager> _detectManagers = new List<BeatManager>();
        public List<BeatManager> DetectManagers
        {
            get { return _detectManagers; }
        }

        public DSAudioSampler(CaptureOption opt)
        {
            _captureOpt = opt;
            double samplePerSec = opt.SamplePerSec / (double)_sampleStep;
            opt.SamplePerSec = samplePerSec;
            for (int i = 0; i < opt.ChannelNum; i++)
            {
                BeatManager dm = new BeatManager(opt);
                _detectManagers.Add(dm);
            }
        }

        public int BufferCB(double SampleTime, IntPtr pBuffer, int BufferLen)
        {
            short[] frameArray = new short[(long)(BufferLen / (double)_captureOpt.ChannelNum)];
            long len = frameArray.Length;
            long channelLen = (long)Math.Ceiling(len / (double)(_sampleStep * _captureOpt.ChannelNum));
            Marshal.Copy(pBuffer, frameArray, 0, frameArray.Length);

            long startTick = (long)(Environment.TickCount - SampleTime * 1000);

            double[][] channels = new double[_captureOpt.ChannelNum][];

            for (int c = 0; c < _captureOpt.ChannelNum; c++)
            {
                channels[c] = new double[channelLen];
            }

            int channel = 0;
            int i = 0; int step = 0;
            foreach (short s in frameArray)
            {
                if (step == 0)
                {
                    channels[channel][i] = (double)(s / 32768.0);
                }
                channel = (channel + 1) % channels.Length;

                if (channel == 0)
                {
                    step = (step + 1) % _sampleStep;
                }
                if (channel == 0 && step == 0) i++;
            }

            for (int c = 0; c < _captureOpt.ChannelNum; c++)
            {
                _detectManagers[c].AddSample(startTick, channels[c]);
            }

            return 0;
        }

        public int SampleCB(double SampleTime, IMediaSample pSample)
        {
            return 0;
        }
    }
}
