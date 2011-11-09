using System;
using System.Collections.Generic;
using System.Text;
using System.IO;

namespace BeatDancer
{
    enum LogType
    {
        Info,
        Warning,
        Error
    }
    class Logger : IDisposable
    {

        public static string FilePath
        {
            get
            {
                string dirPath = Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData);
                dirPath = Path.Combine(dirPath, "BeatDancer");
                if (!Directory.Exists(dirPath))
                {
                    Directory.CreateDirectory(dirPath);
                }
                return Path.Combine(dirPath, "log.txt");
            }
        }

        private static Logger _instance = null;
        public static Logger Instance
        {
            get
            {
                if (_instance == null)
                {
                    _instance = new Logger();
                }
                return _instance;
            }
        }

        private TextWriter _writer = null;

        private Logger()
        {
            try
            {
                _writer = new StreamWriter(FilePath, false, Encoding.UTF8);
                _writer.WriteLine("==== 起動 =========================================================");
                _writer.WriteLine("  " + DateTime.Now.ToString("yyyy/MM/dd HH:mm:ss.fff"));
                _writer.WriteLine("===================================================================");
            }
            catch (Exception ex)
            {
                System.Windows.MessageBox.Show("ログファイルの生成に失敗しました\r\n" + ex.Message);
            }
        }

        public void Dispose()
        {
            if (_writer != null)
            {
                _writer.WriteLine("==== 終了 =========================================================");
                _writer.WriteLine("  " + DateTime.Now.ToString("yyyy/MM/dd HH:mm:ss.fff"));
                _writer.WriteLine("===================================================================");
                _writer.Flush();
                _writer.Close();
            }
        }

        public void Log(LogType lt, string message)
        {
            DateTime dt = DateTime.Now;
            string s = "(" + dt.ToString("yyyy/MM/dd HH:mm:ss.fff") + ") [" + lt.ToString() + "]\t" + message;
            _writer.WriteLine(s);
            _writer.Flush();
        }
    }
}
