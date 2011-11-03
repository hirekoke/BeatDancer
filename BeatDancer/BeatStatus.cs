using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace BeatDancer
{
    class BeatStatus
    {
        /// <summary>最初のサンプルのtick(ミリ秒単位)</summary>
        public long SampleStartTick;

        /// <summary>調整前BPM</summary>
        public double BpmRaw = -1;

        /// <summary>Dancerに調整したBPM</summary>
        public double BpmDancer = -1;

        /// <summary>BpmDancerから算出したbeat時刻(ミリ秒単位)</summary>
        public double BeatTickRaw = -1;

        /// <summary>smoothing後のbeat時刻(ミリ秒単位)</summary>
        public double BeatTickSmooth = -1;
    }

    class DetectStatus
    {
        /// <summary>1秒辺りのサンプル数</summary>
        public double SamplePerSec;

        public double MinBpm = 60;
        public double MaxBpm = 240;
        public double BpmAcc = 1;
        public double[] BpmEnergies;

        public DetectStatus()
        {
            BpmEnergies = new double[(long)Math.Ceiling((MaxBpm + 1 - MinBpm) / BpmAcc)];
            lock (BpmEnergies)
            {
                for (int i = 0; i < BpmEnergies.Length; i++)
                {
                    BpmEnergies[i] = 0;
                }
            }
        }
    }
}
