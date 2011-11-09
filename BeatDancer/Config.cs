using System;
using System.Collections.Generic;
using System.Text;
using System.IO;
using System.Xml;

namespace BeatDancer
{
    using DancerConfig = Dictionary<string, string>;

    class Config
    {
        private System.Windows.Point _windowLocation = new System.Windows.Point(0, 0);
        public System.Windows.Point WindowLocation
        {
            get { return _windowLocation; }
            set { _windowLocation = value; }
        }

        private bool _topMost = false;
        public bool TopMost { get { return _topMost; } set { _topMost = value; } }

        private bool _useCapturedBpm = true;
        public bool UseCapturedBpm
        {
            get { return _useCapturedBpm; }
            set { _useCapturedBpm = value; }
        }
        private double _constBpmValue = 60;
        public double ConstBpmValue
        {
            get { return _constBpmValue; }
            set { _constBpmValue = value; }
        }

        private string _dancerTypeName = "TestDancer";
        public string DancerTypeName
        {
            get { return _dancerTypeName; }
            set { _dancerTypeName = value; }
        }

        private Dictionary<string, DancerConfig> _dancerConfigs = new Dictionary<string, DancerConfig>();
        public Dictionary<string, DancerConfig> DancerConfigs
        {
            get { return _dancerConfigs; }
        }

        private static Config _instance = null;
        public static Config Instance
        {
            get
            {
                if (_instance == null)
                {
                    string filePath = FilePath;
                    if (File.Exists(filePath))
                        _instance = read(FilePath);
                    else
                        _instance = new Config();
                    if (_instance == null) _instance = new Config();
                }
                return _instance;
            }
        }

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
                return Path.Combine(dirPath, "config.xml");
            }
        }

        public DancerConfig GetDancerConfig(string typeName)
        {
            if (_dancerConfigs.ContainsKey(typeName))
            {
                return _dancerConfigs[typeName];
            }
            else
            {
                DancerConfig dc = new DancerConfig();
                _dancerConfigs.Add(typeName, dc);
                return dc;
            }
        }

        private Config()
        {
        }

        public void Save()
        {
            write(FilePath);
        }

        private void write(string filePath)
        {
            try
            {
                XmlWriterSettings settings = new XmlWriterSettings();
                settings.CloseOutput = true;
                settings.CheckCharacters = true;
                settings.ConformanceLevel = ConformanceLevel.Document;
                settings.Encoding = Encoding.UTF8;
                settings.Indent = true;
                settings.IndentChars = "\t";
                settings.NewLineChars = "\r\n";

                using (XmlWriter writer = XmlWriter.Create(filePath, settings))
                {
                    writer.WriteStartDocument();
                    writer.WriteStartElement("Config");

                    writer.WriteStartElement("WindowLocation");
                    writer.WriteElementString("X", WindowLocation.X.ToString());
                    writer.WriteElementString("Y", WindowLocation.Y.ToString());
                    writer.WriteEndElement();

                    writer.WriteElementString("TopMost", TopMost ? "1" : "0");

                    writer.WriteElementString("UseCapturedBpm", UseCapturedBpm ? "1" : "0");
                    writer.WriteElementString("ConstBpmValue", ConstBpmValue.ToString());

                    writer.WriteElementString("DancerTypeName", DancerTypeName);

                    writer.WriteStartElement("DancerConfig");
                    foreach (KeyValuePair<string, DancerConfig> dckv in DancerConfigs)
                    {
                        writer.WriteStartElement(dckv.Key);
                        foreach (KeyValuePair<string, string> kv in dckv.Value)
                        {
                            writer.WriteElementString(kv.Key, kv.Value);
                        }
                        writer.WriteEndElement();
                    }
                    writer.WriteEndElement();

                    writer.WriteEndElement();
                    writer.WriteEndDocument();
                }
            }
            catch (Exception ex)
            {
                Logger.Instance.Log(LogType.Error, "設定ファイルの書き込みに失敗: " + ex.Message + "\r\n" + ex.StackTrace);
            }
        }

        private static Config read(string filePath)
        {
            try
            {
                Config c = new Config();

                XmlDocument doc = new XmlDocument();
                doc.Load(filePath);

                XmlNodeList lst = doc.GetElementsByTagName("Config");
                if (lst.Count == 0) return c;
                XmlNode root = lst[0];

                foreach (XmlNode node in root.ChildNodes)
                {
                    switch (node.Name)
                    {
                        case "WindowLocation":
                            double wlX = 0; double wlY = 0;
                            foreach (XmlNode wlNode in node.ChildNodes)
                            {
                                switch (wlNode.Name)
                                {
                                    case "X":
                                        if (!double.TryParse(wlNode.InnerText.Trim(), out wlX)) wlX = 0;
                                        break;
                                    case "Y":
                                        if (!double.TryParse(wlNode.InnerText.Trim(), out wlY)) wlY = 0;
                                        break;
                                }
                            }
                            c.WindowLocation = new System.Windows.Point(wlX, wlY);
                            break;

                        case "TopMost":
                            c.TopMost = node.InnerText.Trim() != "0";
                            break;

                        case "UseCapturedBpm":
                            c.UseCapturedBpm = node.InnerText.Trim() != "0";
                            break;

                        case "ConstBpmValue":
                            double bpm = 60;
                            if (!double.TryParse(node.InnerText.Trim(), out bpm)) bpm = 60;
                            c.ConstBpmValue = bpm;
                            break;

                        case "DancerTypeName":
                            c.DancerTypeName = node.InnerText.Trim();
                            break;

                        case "DancerConfig":
                            foreach (XmlNode dcNode in node.ChildNodes)
                            {
                                string dancerName = dcNode.Name;
                                if (c.DancerConfigs.ContainsKey(dancerName)) continue;
                                DancerConfig dc = new DancerConfig();
                                foreach (XmlNode dcValues in dcNode.ChildNodes)
                                {
                                    if (!dc.ContainsKey(dcValues.Name))
                                        dc.Add(dcValues.Name, dcValues.InnerText.Trim());
                                }
                                c.DancerConfigs.Add(dancerName, dc);
                            }
                            break;
                    }
                }

                return c;
            }
            catch (Exception ex)
            {
                Logger.Instance.Log(LogType.Error, "設定ファイルの読み込みに失敗: " + ex.Message + "\r\n" + ex.StackTrace);
                return null;
            }
        }
    }
}
