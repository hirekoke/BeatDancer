using System;
using System.Collections.Generic;
using System.Text;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.IO;

namespace BeatDancer
{

    class ImageDancer : Dancer
    {
        private double _minBpm = 70;
        private double _maxBpm = 180;
        public double MinBpm { get { return _minBpm; } set { _minBpm = value; } }
        public double MaxBpm { get { return _maxBpm; } set { _maxBpm = value; } }
        public bool HasConfig { get { return true; } }

        private DrawingGroup rDg = null;
        private DrawingGroup gDg = null;

        private string _dir;
        private List<Image> _images = null;

        public ImageDancer(string directory)
        {
            _dir = directory;
            _images = new List<Image>();
        }

        public void Init(Canvas canvas)
        {
            _images.Clear();

            canvas.Children.Clear();
            canvas.Background = Brushes.Transparent;

            if (Directory.Exists(_dir))
            {
                DirectoryInfo di = new DirectoryInfo(_dir);
                foreach (FileInfo fi in di.GetFiles())
                {
                    loadImage(fi.FullName, canvas);
                }
            }

            rDg = new DrawingGroup();
            Image rimg = new Image();
            rimg.Width = 200; rimg.Height = 200;
            rimg.Source = new DrawingImage(rDg);
            Canvas.SetLeft(rimg, 0);
            Canvas.SetTop(rimg, 0);
            canvas.Children.Add(rimg);

            gDg = new DrawingGroup();
            Image gimg = new Image();
            gimg.Width = 200; gimg.Height = 200;
            gimg.Source = new DrawingImage(gDg);
            Canvas.SetLeft(gimg, 0);
            Canvas.SetTop(gimg, 0);
            canvas.Children.Add(gimg);

            RenderBpmGraph(new double[0]);
        }

        private void loadImage(string fn, Canvas canvas)
        {
            Image img = new Image();
            BitmapImage bmp = new BitmapImage(new Uri(fn, UriKind.Relative));

            img.Source = bmp;
            img.Width = bmp.Width;
            img.Height = bmp.Height;
            Canvas.SetLeft(img, 0);
            Canvas.SetTop(img, 0);
            canvas.Children.Add(img);

            _images.Add(img);
        }

        public void Dispose()
        {
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

            using (DrawingContext dc = gDg.Open())
            {
                if (maxe - mine > 0)
                {
                    double e = maxe - mine;

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
            if (_images.Count == 0)
            {
                using (DrawingContext dc = rDg.Open())
                {
                    Pen framePen = new Pen(Brushes.Red, 3);
                    dc.DrawRectangle(Brushes.White, framePen,
                        new Rect(0, 0, 200, 200));
                    dc.DrawLine(framePen, new Point(0, 0), new Point(200, 200));
                    dc.DrawLine(framePen, new Point(0, 200), new Point(200, 0));

                    //FormattedText noImageText = new FormattedText(
                    //    "画像無いよ", System.Globalization.CultureInfo.CurrentCulture,
                    //    FlowDirection.LeftToRight, new Typeface("メイリオ"), 24, Brushes.Red);
                    //dc.DrawText(noImageText, new Point(10, 40));

                    FormattedText text = new FormattedText(
                        bpm.ToString(), System.Globalization.CultureInfo.CurrentCulture,
                        FlowDirection.LeftToRight, new Typeface("Consolas"), 32, Brushes.Blue);
                    dc.DrawText(text, new Point(10, 10));
                }
            }
            else
            {
                int idx = (int)(ratio * _images.Count);
                idx = idx < 0 ? 0 : (idx >= _images.Count ? _images.Count - 1 : idx);
                for (int i = 0; i < _images.Count; i++)
                {
                    Image img = _images[i];
                    img.Visibility = idx == i ? Visibility.Visible : Visibility.Hidden;
                }

                using (DrawingContext dc = rDg.Open())
                {
                    FormattedText text = new FormattedText(
                        bpm.ToString(), System.Globalization.CultureInfo.CurrentCulture,
                        FlowDirection.LeftToRight, new Typeface("Consolas"), 32, Brushes.Blue);
                    dc.DrawText(text, new Point(10, 10));
                }
            }
        }


        public void Configuration()
        {
            ImageDancerConfig cfg = new ImageDancerConfig();
            bool? result = cfg.ShowDialog();
            if (result.Value)
            {

            }
        }
    }
}
