using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using System.Windows.Shapes;
using System.Windows.Threading;

namespace BeatDancer
{
    /// <summary>
    /// MainWindow.xaml の相互作用ロジック
    /// </summary>
    public partial class MainWindow : Window
    {
        public MainWindow()
        {
            InitializeComponent();

            CompositionTarget.Rendering += new EventHandler(CompositionTarget_Rendering);
            this.Loaded += new RoutedEventHandler(MainWindow_Loaded);

            /// Windowドラッグ
            this.MouseLeftButtonDown += new MouseButtonEventHandler(MainWindow_MouseDown);
            this.MouseMove += new MouseEventHandler(MainWindow_MouseMove);
            this.MouseLeftButtonUp += new MouseButtonEventHandler(MainWindow_MouseUp);
            this.MouseLeave += new MouseEventHandler(MainWindow_MouseLeave);

            this.ContextMenuOpening += new ContextMenuEventHandler(MainWindow_ContextMenuOpening);
            this.ContextMenuClosing += new ContextMenuEventHandler(MainWindow_ContextMenuClosing);
            
            /// 透明化
            this.AllowsTransparency = true;
            this.Background = Brushes.Transparent;
            this.WindowStyle = System.Windows.WindowStyle.None;
        }

        private Capture _cap = null;
        private DetectManager _dm = null;
        private Dancer _dancer = null;

        #region frame rate 関係の変数
        private long _nextTick;
        private long _lastCountTick;
        private long _lastFpsTick;
        private long _currentTick;
        private int _frameCount = 0;
        private double _frameRate;
        private const double _idealFrameRate = 30;
        #endregion


        #region Windowドラッグ
        private bool _isWindowDragging = false;
        private Point _prevPoint = new Point(-1, -1);
        void MainWindow_MouseUp(object sender, MouseButtonEventArgs e)
        {
            _isWindowDragging = false;
        }
        void MainWindow_MouseLeave(object sender, MouseEventArgs e)
        {
            _isWindowDragging = false;
        }

        void MainWindow_MouseMove(object sender, MouseEventArgs e)
        {  
            if (_isWindowDragging && e.LeftButton == MouseButtonState.Pressed)
            {
                Point p = this.PointToScreen(e.GetPosition(this));
                Console.WriteLine("mouse move " + p );
                this.Left += p.X - _prevPoint.X;
                this.Top += p.Y - _prevPoint.Y;
                _prevPoint = p;
            }
        }

        void MainWindow_MouseDown(object sender, MouseButtonEventArgs e)
        {
            if (e.LeftButton == MouseButtonState.Pressed)
            {
                _isWindowDragging = true;
                _prevPoint = this.PointToScreen(e.GetPosition(this));
                Console.WriteLine("mouse down " + _prevPoint);
            }
        }
        #endregion


        void MainWindow_Loaded(object sender, RoutedEventArgs e)
        {
            this.Closing += new System.ComponentModel.CancelEventHandler(MainWindow_Closing);

            /// キャプチャ
            _cap = new Capture();
            if (_cap.Devices.Length == 0)
            {
                return;
            }

            _cap.CapDevice = _cap.Devices[0];
            _cap.CreateGraph();
            _cap.StartCapture();
            _dm = _cap.Sampler.DetectManagers[0];
            DataContext = _dm;

            /// frame rate 初期化
            _currentTick = Environment.TickCount;
            _nextTick = _currentTick;
            _lastCountTick = _currentTick;
            _lastFpsTick = _currentTick;

            /// 踊り手さんスタンバイ
            _dancer = new ImageDancer("hoge");
            _dancer.Init(canvas);
        }

        // 描画更新
        void CompositionTarget_Rendering(object sender, EventArgs e)
        {
            /// 描画が多くなり過ぎないように制御しつつ描画

            _currentTick = Environment.TickCount;

            double diffms = Math.Floor(1000.0 / _idealFrameRate);

            if (_currentTick < _nextTick)
            {
                // 描画 skip
            }
            else
            {
                // render
                if (_dancer == null || _dm == null) return;
                _dancer.Render(_dm.Bpm, _dm.BeatRatio);

                _frameCount++;
                _lastCountTick = _currentTick;
                while (_currentTick >= _nextTick)
                {
                    _nextTick += (long)diffms;
                }
            }

            // frame rate 計算
            if (_currentTick - _lastFpsTick >= 1000)
            {
                _dancer.RenderBpmGraph(_dm.BpmEnergies);

                _frameRate = _frameCount * 1000 / (double)(_currentTick - _lastFpsTick);
                _frameCount = 0;
                _lastFpsTick = _currentTick;
            }
        }

        // 終了処理
        void MainWindow_Closing(object sender, System.ComponentModel.CancelEventArgs e)
        {
            _cap.Dispose();
        }


        void MainWindow_ContextMenuOpening(object sender, ContextMenuEventArgs e)
        {
            dancerConfigMenuItem.IsEnabled = _dancer.HasConfig;
        }

        void MainWindow_ContextMenuClosing(object sender, ContextMenuEventArgs e)
        {
        }


        // 終了
        private void exitMenuItem_Click(object sender, RoutedEventArgs e)
        {
            Application.Current.Shutdown();
        }

        private void dancerConfigMenuItem_Click(object sender, RoutedEventArgs e)
        {
            _dancer.Configuration();
        }

        private void imageDancerSelect_Click(object sender, RoutedEventArgs e)
        {
            _dancer = new ImageDancer("test");
            _dancer.Init(canvas);
        }

        private void testDancerSelect_Click(object sender, RoutedEventArgs e)
        {
            _dancer = new TestDancer();
            _dancer.Init(canvas);
        }

    }
}
