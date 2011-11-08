using System;
using System.Collections.Generic;
using System.Text;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;

namespace BeatDancer.TestDancer
{

    /// <summary>
    ///  デバッグ用
    /// </summary>
    class TestDancer : IDancer
    {
        private const string TYPENAME = "TestDancer";
        private const string NAME = "デバッグ用";

        private double _minBpm = 60;
        private double _maxBpm = 240;
        public double MinBpm { get { return _minBpm; } set { _minBpm = value; } }
        public double MaxBpm { get { return _maxBpm; } set { _maxBpm = value; } }
        public bool HasConfig { get { return true; } }
        public string TypeName { get { return TYPENAME; } }
        public string Name { get { return NAME; } }

        public double Width { get { return 200; } }
        public double Height { get { return 200; } }

        private DrawingGroup rDg = null;
        private DrawingGroup gDg = null;

        public TestDancer()
        {
        }

        public void Init(Canvas canvas)
        {
            canvas.Children.Clear();
            canvas.Background = Brushes.Transparent;

            gDg = new DrawingGroup();
            Image gimg = new Image();
            gimg.Width = 200; gimg.Height = 200;
            gimg.Source = new DrawingImage(gDg);
            Canvas.SetLeft(gimg, 0);
            Canvas.SetTop(gimg, 0);
            canvas.Children.Add(gimg);

            rDg = new DrawingGroup();
            Image img = new Image();
            img.Width = 200; img.Height = 200;
            img.Source = new DrawingImage(rDg);
            Canvas.SetLeft(img, 0);
            Canvas.SetTop(img, 0);
            canvas.Children.Add(img);

            RenderBpmGraph(new double[0]);
        }

        public void RenderBpmGraph(double[] energies)
        {
            double maxe = double.MinValue;
            double mine = double.MaxValue;
            for (int i = 0; i < energies.Length; i++)
            {
                if (maxe < energies[i]) maxe = energies[i];
                if (mine > energies[i]) mine = energies[i];
            }
            double e = maxe - mine;

            using (DrawingContext dc = gDg.Open())
            {
                dc.DrawRectangle(new SolidColorBrush(Color.FromArgb(128, 255, 255, 255)),
                    null, new Rect(0, 0, 200, 200));
                if (e > 0)
                {
                    for (int i = 1; i < energies.Length; i++)
                    {
                        double x0 = (200 / (double)energies.Length) * (i - 1);
                        double y0 = 200 - (energies[i - 1] - mine) * 200 / e;
                        double x1 = (200 / (double)energies.Length) * i;
                        double y1 = 200 - (energies[i] - mine) * 200 / e;

                        dc.DrawLine(new Pen(Brushes.Black, 1.0), new Point(x0, y0), new Point(x1, y1));
                    }
                }
            }
        }

        public void Render(double bpm, double ratio)
        {
            using (DrawingContext dc = rDg.Open())
            {
                dc.DrawRectangle(Brushes.Transparent, null, new Rect(0, 0, 200, 200));
                dc.DrawRectangle(Brushes.Red, null,
                    new Rect(180 * (ratio <= 0.5 ? ratio : 1.0 - ratio) * 2,
                        200 / 2.0 - 10, 20, 20));

                FormattedText text = new FormattedText(
                    bpm.ToString(), System.Globalization.CultureInfo.CurrentCulture,
                    FlowDirection.LeftToRight, new Typeface("Consolas"), 32, Brushes.Blue);
                dc.DrawText(text, new Point(10, 10));
            }
        }


        public void ConvertFromDic(ref Dictionary<string, string> dic)
        {
            if (dic.ContainsKey("MinBpm"))
                double.TryParse(dic["MinBpm"], out _minBpm);
            if (dic.ContainsKey("MaxBpm"))
                double.TryParse(dic["MaxBpm"], out _maxBpm);
        }

        public void ConvertToDic(ref Dictionary<string, string> dic)
        {
            if (dic.ContainsKey("MinBpm"))
                dic["MinBpm"] = MinBpm.ToString();
            else
                dic.Add("MinBpm", MinBpm.ToString());
            if (dic.ContainsKey("MaxBpm"))
                dic["MaxBpm"] = MaxBpm.ToString();
            else
                dic.Add("MaxBpm", MaxBpm.ToString());
        }


        public void Dispose()
        {
        }


        public void Configuration()
        {
            MessageBox.Show("設定ダイアログオープン");
        }

    }
}
