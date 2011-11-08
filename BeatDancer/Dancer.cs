using System;
using System.Collections.Generic;
using System.Text;
using System.Windows;
using System.Windows.Controls;

namespace BeatDancer
{
    interface IDancer : IDisposable
    {
        double MinBpm { get; set; }
        double MaxBpm { get; set; }
        string TypeName { get; }
        string Name { get; }
        double Width { get; }
        double Height { get; }
        bool HasConfig { get; }
        void Init(Canvas canvas);
        void Configuration();
        void Render(double bpm, double ratio);
        void RenderBpmGraph(double[] energies);

        void ConvertFromDic(ref Dictionary<string, string> dic);
        void ConvertToDic(ref Dictionary<string, string> dic);
    }
}
