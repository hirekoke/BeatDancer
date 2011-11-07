using System;
using System.Collections.Generic;
using System.Text;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.IO;

using System.Xml;

namespace BeatDancer.ImageDancer
{
    class ImageDancerConfig
    {
        private string _imageDirPath;
        public string ImageDirPath { get { return _imageDirPath; } set { _imageDirPath = value; } }
        private bool _showBpm;
        public bool ShowBpm { get { return _showBpm; } set { _showBpm = value; } }
        private bool _showGraph;
        public bool ShowGraph { get { return _showGraph; } set { _showGraph = value; } }
        public ImageDancerConfig Copy()
        {
            ImageDancerConfig ret = new ImageDancerConfig();
            ret.ImageDirPath = this.ImageDirPath;
            ret.ShowBpm = this.ShowBpm;
            ret.ShowGraph = this.ShowGraph;
            return ret;
        }
    }

    /// <summary>
    /// 画像ファイルが入ったフォルダを指定する踊り手
    ///   ・ファイル名が番号のとき
    ///       番号が拍内位置の割合を表す
    ///       最初の番号と最後の番号は共に0の位置(最後のファイルはファイル名しか使用されない はず)
    ///   ・ファイル名が番号でないとき
    ///       順番に使用される
    ///       最後のファイルまで使用される
    /// </summary>
    class ImageDancer : Dancer
    {
        private const string TYPENAME = "ImageDancer";
        private const string NAME = "画像";
        private static string[] _allowImageTypes = { "bmp", "png", "jpg", "gif" };

        private double _minBpm = 70;
        private double _maxBpm = 180;
        public double MinBpm { get { return _minBpm; } set { _minBpm = value; } }
        public double MaxBpm { get { return _maxBpm; } set { _maxBpm = value; } }
        public bool HasConfig { get { return true; } }
        public string TypeName { get { return TYPENAME; } }
        public string Name { get { return NAME; } }

        private double _width = 200;
        private double _height = 200;

        private DrawingGroup rDg = null;
        private DrawingGroup gDg = null;

        private ImageDancerConfig _config = new ImageDancerConfig();

        private List<Image> _images = null;
        private List<double> _imageNumbers = null;
        private bool _allNumbers = false;
        private Canvas _canvas = null;

        private bool _render = true;

        public ImageDancer()
        {
            _images = new List<Image>();
            _imageNumbers = new List<double>();
        }

        public void Init(Canvas canvas)
        {
            _canvas = canvas;
            _images.Clear();
            _imageNumbers.Clear();

            canvas.Children.Clear();
            canvas.Background = Brushes.Transparent;
            _width = 0; _height = 0;

            gDg = new DrawingGroup();
            Image gimg = new Image();
            gimg.Source = new DrawingImage(gDg);
            Canvas.SetLeft(gimg, 0);
            Canvas.SetTop(gimg, 0);
            canvas.Children.Add(gimg);

            rDg = new DrawingGroup();
            Image rimg = new Image();
            rimg.Source = new DrawingImage(rDg);
            Canvas.SetLeft(rimg, 0);
            Canvas.SetTop(rimg, 0);
            canvas.Children.Add(rimg);

            if (Directory.Exists(_config.ImageDirPath))
            {
                _allNumbers = true;
                DirectoryInfo di = new DirectoryInfo(_config.ImageDirPath);
                FileInfo[] fs = di.GetFiles();
                Array.Sort(fs, (FileInfo fi1, FileInfo fi2) => { return fi1.Name.CompareTo(fi2.Name); });

                foreach (FileInfo fi in fs)
                {
                    string ext = fi.Extension;
                    if (ext.StartsWith(".")) ext = ext.Substring(1);
                    if (Array.IndexOf(_allowImageTypes, ext) >= 0)
                    {
                        loadImage(fi.FullName, canvas);
                        string fn = Path.GetFileNameWithoutExtension(fi.Name);
                        double fd = -1;
                        if (_allNumbers && double.TryParse(fn, out fd))
                        {
                            _imageNumbers.Add(fd);
                        }
                        else
                        {
                            _allNumbers = false;
                        }
                    }
                }
            }
            if (_allNumbers)
            {
                double min = _imageNumbers[0];
                double max = _imageNumbers[_imageNumbers.Count - 1];
                if (max - min <= 0)
                {
                    _allNumbers = false;
                }
                else
                {
                    for (int i = 0; i < _imageNumbers.Count; i++)
                    {
                        _imageNumbers[i] = (_imageNumbers[i] - min) / (max - min);
                    }
                }
            }
            if (_images.Count <= 0)
            {
                _width = 200; _height = 200;
            }
            gimg.Width = _width; gimg.Height = 200;
            rimg.Width = _width; rimg.Height = _height;

            RenderBpmGraph(new double[0]);
        }

        private void loadImage(string fn, Canvas canvas)
        {
            Image img = new Image();
            BitmapImage bmp = new BitmapImage(new Uri(fn, UriKind.Relative));
            img.Source = bmp;
            img.Width = bmp.Width; img.Height = bmp.Height;
            if (img.Width > _width) _width = img.Width;
            if (img.Height > _height) _height = img.Height;
            img.Tag = Path.GetFileNameWithoutExtension(fn);

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
            if (!_config.ShowGraph) return;
            lock (this)
            {
                if (!_render) return;
            }

            double maxe = double.MinValue;
            double mine = double.MaxValue;
            for (int i = 0; i < energies.Length; i++)
            {
                if (maxe < energies[i]) maxe = energies[i];
                if (mine > energies[i]) mine = energies[i];
            }

            using (DrawingContext dc = gDg.Open())
            {
                dc.DrawRectangle(new SolidColorBrush(Color.FromArgb(127, 0, 0, 0)), null, new Rect(0, 0, _width, 200));
                if (maxe - mine > 0)
                {
                    double e = maxe - mine;

                    for (int i = 1; i < energies.Length; i++)
                    {
                        double x0 = (_width / (double)energies.Length) * (i - 1);
                        double y0 = 200 - (energies[i - 1] - mine) * 200 / e;
                        double x1 = (_width / (double)energies.Length) * i;
                        double y1 = 200 - (energies[i] - mine) * 200 / e;

                        dc.DrawLine(new Pen(Brushes.White, 1.5), new Point(x0, y0), new Point(x1, y1));
                    }
                }
            }
        }

        private void drawBpm(double bpm, DrawingContext dc)
        {
            FormattedText text = new FormattedText(
                bpm.ToString(), System.Globalization.CultureInfo.CurrentCulture,
                FlowDirection.LeftToRight, new Typeface("Consolas"), 32, Brushes.Blue);
            Geometry g = text.BuildGeometry(new Point(10, 10));
            PathGeometry pg = g.GetOutlinedPathGeometry();
            dc.DrawGeometry(Brushes.Blue, new Pen(Brushes.White, 4), g);
            dc.DrawGeometry(Brushes.Blue, null, g);
        }

        public void Render(double bpm, double ratio)
        {
            lock (this)
            {
                if (!_render) return;
            }

            if (_images.Count == 0)
            {
                using (DrawingContext dc = rDg.Open())
                {
                    Pen framePen = new Pen(Brushes.Red, 3);
                    dc.DrawRectangle(Brushes.White, framePen,
                        new Rect(0, 0, _width, _height));
                    dc.DrawLine(framePen, new Point(0, 0), new Point(_width, _height));
                    dc.DrawLine(framePen, new Point(0, _height), new Point(_width, 0));

                    if (_config.ShowBpm) drawBpm(bpm, dc);
                }
            }
            else
            {
                // 描画する画像の決定
                int idx = -1;
                if (_allNumbers)
                {
                    // ファイル名が全部番号の場合: 数字が拍の割合になるように割り振る
                    for (int i = 0; i < _imageNumbers.Count; i++)
                    {
                        if (ratio < _imageNumbers[i])
                        {
                            idx = i - 1;
                            break;
                        }
                    }
                }
                else
                {
                    // ファイル名が全部番号ではない場合: 均等に割り振る
                    idx = (int)(ratio * _images.Count);
                }
                idx = idx < 0 ? 0 : (idx >= _images.Count ? _images.Count - 1 : idx);
                // 画像表示
                for (int i = 0; i < _images.Count; i++)
                {
                    Image img = _images[i];
                    img.Visibility = idx == i ? Visibility.Visible : Visibility.Hidden;
                }

                using (DrawingContext dc = rDg.Open())
                {
                    dc.DrawRectangle(Brushes.Transparent, null, new Rect(0, 0, _width, _height));
                    if (_config.ShowBpm) drawBpm(bpm, dc);
                }
            }
        }


        public void Configuration()
        {
            lock (this)
            {
                _render = false;
            }
            ImageDancerConfigWindow cfg = new ImageDancerConfigWindow();
            cfg.DataContext = _config.Copy();
            bool? result = cfg.ShowDialog();
            if (result.Value)
            {
                this._config = (ImageDancerConfig)cfg.DataContext;
                this.Init(_canvas);
                Dictionary<string, string> dic = Config.Instance.GetDancerConfig(this.TypeName);
                this.ConvertToDic(ref dic);
            }
            lock (this)
            {
                _render = true;
            }
        }

        public void ConvertFromDic(ref Dictionary<string, string> dic)
        {
            if (dic.ContainsKey("MinBpm"))
                double.TryParse(dic["MinBpm"], out _minBpm);
            if (dic.ContainsKey("MaxBpm"))
                double.TryParse(dic["MaxBpm"], out _maxBpm);
            if (dic.ContainsKey("ImageDirPath"))
                _config.ImageDirPath = dic["ImageDirPath"];
            if (dic.ContainsKey("ShowBpm"))
                _config.ShowBpm = dic["ShowBpm"] != "0";
            if (dic.ContainsKey("ShowGraph"))
                _config.ShowGraph = dic["ShowGraph"] != "0";
        }

        public void ConvertToDic(ref Dictionary<string, string> dic)
        {
            addDic(ref dic, "MinBpm", MinBpm.ToString());
            addDic(ref dic, "MaxBpm", MaxBpm.ToString());

            addDic(ref dic, "ImageDirPath", _config.ImageDirPath);
            addDic(ref dic, "ShowBpm", _config.ShowBpm ? "1" : "0");
            addDic(ref dic, "ShowGraph", _config.ShowGraph ? "1" : "0");
        }
        private void addDic(ref Dictionary<string, string> dic, string key, string value)
        {
            if (dic.ContainsKey(key))
                dic[key] = value;
            else
                dic.Add(key, value);
        }
    }
}
