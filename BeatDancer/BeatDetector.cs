using System;
using System.Collections.Generic;
using System.Text;
using AForge.Math;

namespace BeatDancer
{
    /// <summary>
    /// Beat検出
    /// アルゴリズム参考 : http://www.clear.rice.edu/elec301/Projects01/beat_sync/beatalgo.html
    /// </summary>
    class BeatDetector
    {
        private static double[] _bandLimits = { 0, 200, 400, 800, 1600, 3200 };
        private const double _maxFreq = 4096;

        /// number of pulses in the comb filter
        private const int _npulses = 3;

        // decay of energies ( <= 1.0 )
        private const double _energyDecay = 0.6;

        public BeatDetector()
        {
        }

        #region BPM算出処理

        public List<double[]> DetectBpm(double[] sig, ref DetectStatus ds, ref BeatStatus bs)
        {
            // 必要数の入力を複素数に変換
            int n = (int)Math.Floor(Math.Log(sig.Length, 2));
            int size = (int)Math.Pow(2, n > 14 ? 14 : n); // limit of AForge.NET

            Complex[] input = new Complex[size];
            for (int i = 0; i < size; i++)
            {
                input[i] = new Complex(sig[i], 0);
            }

            // Bpm検出処理
            List<Complex[]> filtered = filterBank(input);
            List<Complex[]> windowed = hwindow(filtered, 0.2);
            List<Complex[]> differentiated = diffrect(windowed);

            List<Complex[]> dft = timeCombPre(differentiated);
            double rBpm = timeCombRough(dft, ref ds);

            int maxBand = -1;
            double b2 = timeComb(dft, 0.5, rBpm - 1, rBpm + 1, ds.SamplePerSec, ref maxBand);
            bs.BpmRaw = b2;

            List<double[]> features = new List<double[]>();
            for (int i = 0; i < _bandLimits.Length; i++)
            {
                double[] f = Array.ConvertAll<Complex, double>(differentiated[i], c => c.Re);
                features.Add(f);
            }
            //double[] features = Array.ConvertAll<Complex, double>(differentiated[maxBand], c => c.Re);
            return features;
        }

        /// <summary>入力を周波数帯で分ける</summary>
        private List<Complex[]> filterBank(Complex[] sig)
        {
            FourierTransform.FFT(sig, FourierTransform.Direction.Forward);

            int n = sig.Length;
            int nbands = _bandLimits.Length;

            // 各周波数帯の左右の値を得る
            long[] bandL = new long[nbands];
            long[] bandR = new long[nbands];

            for (int i = 0; i < nbands - 1; i++)
            {
                bandL[i] = (long)Math.Floor((_bandLimits[i] / _maxFreq) * (n / 2.0));
                bandR[i] = (long)Math.Floor((_bandLimits[i + 1] / _maxFreq) * (n / 2.0)) - 1;
            }
            bandL[nbands - 1] = (long)Math.Floor((_bandLimits[nbands - 1] / _maxFreq) * (n / 2.0));
            bandR[nbands - 1] = (long)Math.Floor(n / 2.0) - 1;

            // 分ける
            List<Complex[]> ret = new List<Complex[]>();
            for (int i = 0; i < nbands; i++)
            {
                Complex[] tmp = new Complex[n];
                Array.Copy(sig, bandL[i], tmp, bandL[i], bandR[i] - bandL[i]);
                Array.Copy(sig, n - bandR[i], tmp, n - bandR[i], bandR[i] - bandL[i]);
                Array.Reverse(tmp, (int)(n - bandR[i]), (int)(bandR[i] - bandL[i]));
                ret.Add(tmp);
            }
            ret[0][0] = new Complex(0, 0);

            return ret;
        }

        /// <summary>ハン窓で畳み込みをして扱う信号を限定する</summary>
        private List<Complex[]> hwindow(List<Complex[]> sigs, double winLength)
        {
            int n = sigs[0].Length;
            int nbands = _bandLimits.Length;

            /// Create half-Hanning window
            int hannLen = (int)Math.Floor(winLength * 2 * _maxFreq);
            Complex[] hann = new Complex[n];
            for (int i = 0; i < hannLen; i++)
            {
                hann[i] = new Complex(Math.Pow(Math.Cos(0.5 * Math.PI * i / (double)(hannLen - 1)), 2), 0);
            }

            /// Take IFFT to transfrom to time domain.
            List<Complex[]> wave = new List<Complex[]>();
            for (int i = 0; i < nbands; i++)
            {
                FourierTransform.FFT(sigs[i], FourierTransform.Direction.Backward);

                Complex[] tmp = new Complex[sigs[i].Length];
                Complex[] reals = Array.ConvertAll<Complex, Complex>(sigs[i],
                    complex => (new Complex(complex.Re >= 0 ? complex.Re : -complex.Re, 0)));
                Array.Copy(reals, tmp, reals.Length);
                wave.Add(tmp);

                /// Full-wave rectification in the time domain
                /// And back to frequency with FFT
                FourierTransform.FFT(wave[i], FourierTransform.Direction.Forward);
            }

            /// Convolving with half-Hanning same as multiplying in frequency
            /// Multiply half-Hanning FFT by signal FFT
            /// Inverse transform to get output in the time domain
            FourierTransform.FFT(hann, FourierTransform.Direction.Forward);

            List<Complex[]> ret = new List<Complex[]>();
            for (int i = 0; i < nbands; i++)
            {
                Complex[] filtered = new Complex[wave[i].Length];
                for (int j = 0; j < filtered.Length; j++)
                {
                    filtered[j] = wave[i][j] * hann[j];
                }

                FourierTransform.FFT(filtered, FourierTransform.Direction.Backward);
                Complex[] reals = Array.ConvertAll<Complex, Complex>(filtered, complex => new Complex(complex.Re, 0));
                ret.Add(reals);
            }

            return ret;
        }

        /// <summary>信号の差分を取って増加方向の場合のみを残す</summary>
        private List<Complex[]> diffrect(List<Complex[]> sig)
        {
            int nbands = _bandLimits.Length;
            int n = sig[0].Length;

            List<Complex[]> ret = new List<Complex[]>();
            for (int i = 0; i < nbands; i++)
            {
                Complex[] tmp = new Complex[n];
                for (int j = 5; j < n; j++)
                {
                    /// Find the difference from one smaple to the next
                    Complex c = sig[i][j] - sig[i][j - 1];
                    if (c.Re > 0)
                    {
                        /// Retain only if difference is positive (Half-Wave rectify)
                        tmp[j] = c;
                    }
                }
                ret.Add(tmp);
            }
            return ret;
        }

        /// <summary>timecombの前処理</summary>
        private List<Complex[]> timeCombPre(List<Complex[]> sig)
        {
            int n = sig[0].Length;
            int nbands = _bandLimits.Length;

            /// Get signal in frequency domain
            List<Complex[]> dft = new List<Complex[]>();
            for (int i = 0; i < nbands; i++)
            {
                Complex[] tmp = new Complex[sig[i].Length];
                Array.Copy(sig[i], tmp, tmp.Length);
                FourierTransform.FFT(tmp, FourierTransform.Direction.Forward);
                dft.Add(tmp);
            }

            return dft;
        }

        /// <summary>各BPM候補でエネルギーを算出</summary>
        private double timeCombRough(List<Complex[]> dft, ref DetectStatus ds)
        {
            int n = dft[0].Length;
            int nbands = _bandLimits.Length;

            double maxe = 0; double bpmResult = 0;
            int ei = 0;
            for (double bpm = ds.MinBpm; bpm <= ds.MaxBpm; bpm += ds.BpmAcc)
            {
                /// Initialize energy and filter to zero(s)
                double e = 0;
                Complex[] fil = new Complex[n];
                for (int i = 0; i < fil.Length; i++) fil[i] = new Complex(0, 0);

                /// Calculate the difference between peaks in the filter for a certain tempo
                int nstep = (int)Math.Floor(ds.SamplePerSec * 60 / bpm);

                /// Set every nstep samples of the filter to one
                for (int i = 0; i < _npulses && i * nstep < fil.Length; i++)
                {
                    fil[i * nstep] = new Complex(1, 0);
                }

                /// Get the filter in the frequency domain
                FourierTransform.FFT(fil, FourierTransform.Direction.Forward);

                /// Calculate the energy after convolution
                for (int i = 0; i < nbands; i++)
                {
                    double sum = 0;
                    for (int j = 0; j < fil.Length; j++)
                    {
                        Complex c = fil[j] * dft[i][j];
                        sum += Math.Pow(c.Re, 2) + Math.Pow(c.Im, 2);
                    }
                    e += sum * ((i == 0 || i == nbands-1 || i == nbands - 2) ? 1.2 : 1.0);
                }

                /// Set the energy to DetectStatus
                lock (ds.BpmEnergies)
                {
                    ds.BpmEnergies[ei] = (1 - _energyDecay) * ds.BpmEnergies[ei] + _energyDecay * e;
                }
                if (maxe < ds.BpmEnergies[ei])
                {
                    maxe = ds.BpmEnergies[ei];
                    bpmResult = bpm;
                }
                ei++;
            }
            return bpmResult;
        }

        /// <summary>BPM候補を試してエネルギーが大きいものを選択する</summary>
        private double timeComb(
            List<Complex[]> dft, 
            double acc, double minbpm, double maxbpm,
            double samplePerSec,
            ref int maxBand)
        {
            int n = dft[0].Length;
            int nbands = _bandLimits.Length;

            /// Initialize max energy to zero
            double maxe = 0;
            double sbpm = minbpm;

            for (double bpm = minbpm; bpm <= maxbpm; bpm += acc)
            {
                /// Initialize energy and filter to zero(s)
                double e = 0;
                Complex[] fil = new Complex[n];
                for (int i = 0; i < fil.Length; i++) fil[i] = new Complex(0, 0);

                /// Calculate the difference between peaks in the filter for a certain tempo
                int nstep = (int)Math.Floor(samplePerSec * (60 / bpm));

                /// Set every nstep samples of the filter to one
                for (int i = 0; i < _npulses && i * nstep < fil.Length; i++) fil[i * nstep] = new Complex(1000, 0);

                /// Get the filter in the frequency domain
                FourierTransform.FFT(fil, FourierTransform.Direction.Forward);

                /// Calculate the energy after convolution
                int mb = -1; double m = -1;
                for (int i = 0; i < nbands; i++)
                {
                    double sum = 0;
                    for (int j = 0; j < fil.Length; j++)
                    {
                        Complex c = fil[j] * dft[i][j];
                        sum += Math.Pow(c.Re, 2) + Math.Pow(c.Im, 2);
                    }
                    if (m < sum)
                    {
                        mb = i;
                        m = sum;
                    }
                    e += sum * ((i == 0 || i == nbands - 1 || i == nbands - 2) ? 1.2 : 1.0);
                }

                /// If greater than all previous energies, 
                /// set current bpm to the bpm of the signal
                if (e > maxe)
                {
                    sbpm = bpm;
                    maxe = e;
                    maxBand = mb;
                }
            }

            return sbpm;
        }

        #endregion

        #region ビート検出処理
        public void AdjustBeat(List<double[]> features, ref DetectStatus ds, ref BeatStatus bs)
        {
            int nstep = (int)Math.Floor(ds.SamplePerSec * (60 / bs.BpmDancer));

            double maxe = 0; int maxs = -1;
            for (int s = 0; s < nstep; s += 2)
            {
                double e = 0;

                for (int i = 0; i < features.Count; i++)
                {
                    for (int j = 0; j < features[i].Length; j++)
                    {
                        double peakValue = 1 - (j % (s + nstep)) / (double)nstep;
                        e += Math.Pow(features[i][j] * peakValue, 2);
                    }
                }
                if (maxe < e)
                {
                    maxe = e; maxs = s;
                }
            }
            int beatStartSample = maxs;
            bs.BeatTickRaw = bs.SampleStartTick + beatStartSample * 1000 / ds.SamplePerSec;
        }

        #endregion
    }
}
