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
using System.Windows.Shapes;

namespace BeatDancer.ImageDancer
{
    /// <summary>
    /// ImageDancerConfig.xaml の相互作用ロジック
    /// </summary>
    public partial class ImageDancerConfigWindow : Window
    {
        public ImageDancerConfigWindow()
        {
            InitializeComponent();
        }

        private void OK_Click(object sender, RoutedEventArgs e)
        {
            DialogResult = true;
            this.Close();
        }

        private void Cancel_Click(object sender, RoutedEventArgs e)
        {
            DialogResult = false;
            this.Close();
        }

        private void Path_Click(object sender, RoutedEventArgs e)
        {
            System.Windows.Forms.FolderBrowserDialog dlg = new System.Windows.Forms.FolderBrowserDialog();
            dlg.Description = "画像を含むフォルダを選択";
            dlg.RootFolder = Environment.SpecialFolder.Desktop;
            if (string.IsNullOrEmpty(((ImageDancerConfig)DataContext).ImageDirPath))
            {
                dlg.SelectedPath = AppDomain.CurrentDomain.BaseDirectory;
            }
            else
            {
                dlg.SelectedPath = ((ImageDancerConfig)DataContext).ImageDirPath;
            }
            dlg.ShowNewFolderButton = true;
            if (dlg.ShowDialog() == System.Windows.Forms.DialogResult.OK)
            {
                ImageDancerConfig idc = (ImageDancerConfig)DataContext;
                idc.ImageDirPath = dlg.SelectedPath;
                dirPathBox.Text = dlg.SelectedPath;
            }
        }
    }
}
