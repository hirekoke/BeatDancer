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
            Logger.Instance.Log(LogType.Info, "初期化開始");

            InitializeComponent();

            _dancerManager = new DancerManager();
            _dancerManager.CreateMenu(dancerSelectMenu);

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
        private DancerManager _dancerManager = null;
        private BeatManager _beatManager = null;

        #region frame rate 関係の変数
        private long _nextTick;
        private long _lastCountTick;
        private long _lastFpsTick;
        private long _currentTick;
        private int _frameCount = 0;
        private double _frameRate;
        private const double _idealFrameRate = 30;

        private long _constBpmBaseTick = 0;
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
            }
        }
        #endregion


        void MainWindow_Loaded(object sender, RoutedEventArgs e)
        {
            this.Left = Config.Instance.WindowLocation.X;
            this.Top = Config.Instance.WindowLocation.Y;

            this.Closing += new System.ComponentModel.CancelEventHandler(MainWindow_Closing);

            Logger.Instance.Log(LogType.Info, "キャプチャ開始");
            /// キャプチャ
            _cap = new Capture();
            if (_cap.Devices.Length == 0)
            {
                return;
            }
            _cap.CapDevice = _cap.Devices[0];
            _cap.CreateGraph();
            _cap.StartCapture();
            _beatManager = _cap.Sampler.DetectManagers[0];
            DataContext = _beatManager;

            /// メニュー
            captureBpmMenuItem.IsChecked = Config.Instance.UseCapturedBpm;
            constBpmMenuItem.IsChecked = !Config.Instance.UseCapturedBpm;
            constBpmValueBox.Text = Config.Instance.ConstBpmValue.ToString();

            /// frame rate 初期化
            _currentTick = Environment.TickCount;
            _nextTick = _currentTick;
            _lastCountTick = _currentTick;
            _lastFpsTick = _currentTick;
            _constBpmBaseTick = _currentTick;
        }

        // 描画更新
        void CompositionTarget_Rendering(object sender, EventArgs e)
        {
            /// 描画が多くなり過ぎないように制御しつつ描画
            /// 固定BPMの場合それに合わせて描画

            _currentTick = Environment.TickCount;

            double diffms = Math.Floor(1000.0 / _idealFrameRate);

            if (_currentTick < _nextTick)
            {
                // 描画 skip
            }
            else
            {
                if (!_isWindowDragging)
                {
                    // render
                    if (_dancerManager != null && _dancerManager.Dancer != null)
                    {
                        if (_beatManager != null && Config.Instance.UseCapturedBpm)
                        {
                            _dancerManager.Dancer.Render(_beatManager.Bpm, _beatManager.BeatRatio);
                        }
                        else
                        {
                            double ratio = (_currentTick - _constBpmBaseTick) * Config.Instance.ConstBpmValue / 60000.0;
                            ratio = ratio - Math.Truncate(ratio);
                            if (ratio < 0 || ratio >= 1.0) ratio = 0;
                            _dancerManager.Dancer.Render(Config.Instance.ConstBpmValue, ratio);
                        }
                    }
                }

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
                if (!_isWindowDragging)
                {
                    if (_dancerManager != null && _dancerManager.Dancer != null)
                    {
                        if (_beatManager != null && Config.Instance.UseCapturedBpm)
                        {
                            _dancerManager.Dancer.RenderBpmGraph(_beatManager.BpmEnergies);
                        }
                        else
                        {
                            _dancerManager.Dancer.RenderBpmGraph(new double[] { 0 });
                        }
                    }
                }

                _frameRate = _frameCount * 1000 / (double)(_currentTick - _lastFpsTick);
                _frameCount = 0;
                _lastFpsTick = _currentTick;
            }
        }

        // 終了処理
        void MainWindow_Closing(object sender, System.ComponentModel.CancelEventArgs e)
        {
            Config.Instance.WindowLocation = new Point(this.Left, this.Top);
            Config.Instance.Save();
            _cap.Dispose();
        }


        void MainWindow_ContextMenuOpening(object sender, ContextMenuEventArgs e)
        {
            if (_dancerManager != null && _dancerManager.Dancer != null)
                dancerConfigMenuItem.IsEnabled = _dancerManager.Dancer.HasConfig;
            else
                dancerConfigMenuItem.IsEnabled = false;
        }

        void MainWindow_ContextMenuClosing(object sender, ContextMenuEventArgs e)
        {
        }


        // 終了
        private void exitMenuItem_Click(object sender, RoutedEventArgs e)
        {
            Config.Instance.Save();
            Logger.Instance.Dispose();
            Application.Current.Shutdown();
        }

        private void dancerConfigMenuItem_Click(object sender, RoutedEventArgs e)
        {
            if (_dancerManager != null && _dancerManager.Dancer != null)
                _dancerManager.Dancer.Configuration();
        }


        private void constBpmValueBox_KeyDown(object sender, KeyEventArgs e)
        {
            string s = constBpmValueBox.Text;
            double bpm = -1;
            if (double.TryParse(s, out bpm))
            {
                Config.Instance.ConstBpmValue = bpm;
                _constBpmBaseTick = Environment.TickCount;
            }
            else
            {
                constBpmValueBox.Text = Config.Instance.ConstBpmValue.ToString();
            }
        }

        private void constBpmMenuItem_Click(object sender, RoutedEventArgs e)
        {
            constBpmMenuItem.IsChecked = true;
            captureBpmMenuItem.IsChecked = false;
            Config.Instance.UseCapturedBpm = false;
            _constBpmBaseTick = Environment.TickCount;
        }

        private void captureBpmMenuItem_Click(object sender, RoutedEventArgs e)
        {
            captureBpmMenuItem.IsChecked = true;
            constBpmMenuItem.IsChecked = false;
            Config.Instance.UseCapturedBpm = true;
        }
    }
}
