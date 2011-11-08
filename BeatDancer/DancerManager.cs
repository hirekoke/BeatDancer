using System;
using System.Collections.Generic;
using System.Text;
using System.Reflection;
using System.Windows.Controls;

namespace BeatDancer
{
    class DancerManager
    {
        private Dictionary<string, string> _nameTable = null;
        private Dictionary<string, Type> _typeNameTable = null;
        private List<MenuItem> _menuItems = null;

        private IDancer _dancer = null;
        public IDancer Dancer
        {
            get { return _dancer; }
        }

        public DancerManager()
        {
            _nameTable = new Dictionary<string, string>();
            _typeNameTable = new Dictionary<string, Type>();
            foreach (Assembly asm in AppDomain.CurrentDomain.GetAssemblies())
            {
                foreach (Module m in asm.GetModules())
                {
                    if (m.Name.Contains("BeatDancer"))
                    {
                        foreach (Type t in m.GetTypes())
                        {
                            if (t.GetInterface("IDancer") == typeof(IDancer))
                            {
                                object o = Activator.CreateInstance(t);
                                IDancer d = o as IDancer;
                                _nameTable.Add(d.TypeName, d.Name);
                                _typeNameTable.Add(d.TypeName, t);
                            }
                        }
                    }
                }
            }

            SelectDancer(Config.Instance.DancerTypeName);
        }

        public void CreateMenu(MenuItem parentMenu)
        {
            _menuItems = new List<MenuItem>();
            foreach (KeyValuePair<string, Type> kv in _typeNameTable)
            {
                MenuItem mi = new MenuItem();
                mi.Tag = kv.Key;
                if (_nameTable.ContainsKey(kv.Key))
                    mi.Header = _nameTable[kv.Key];
                mi.IsCheckable = true;
                mi.Click += (sender, e) => { SelectDancer(mi.Tag as string); };
                parentMenu.Items.Add(mi);
                _menuItems.Add(mi);
                mi.IsChecked = (_dancer != null && _dancer.TypeName == kv.Key);
            }
        }

        public void SelectDancer(string typeName)
        {
            Logger.Instance.Log(LogType.Info, "踊り手選択: " + typeName);
            if (_typeNameTable.ContainsKey(typeName))
            {
                Config.Instance.DancerTypeName = typeName;

                Type t = _typeNameTable[typeName];
                object o = Activator.CreateInstance(t);
                if (_dancer != null)
                {
                    Dictionary<string, string> dc = Config.Instance.GetDancerConfig(_dancer.TypeName);
                    _dancer.ConvertToDic(ref dc);
                    _dancer.Dispose();
                }
                _dancer = o as IDancer;
                Canvas c = (App.Current.MainWindow as MainWindow).canvas;
                Dictionary<string, string> ndc = Config.Instance.GetDancerConfig(_dancer.TypeName);
                _dancer.ConvertFromDic(ref ndc);
                _dancer.Init(c);

                Config.Instance.Save();

                (App.Current.MainWindow as MainWindow).AdjustWindowPosition();

                if (_menuItems != null)
                {
                    foreach (MenuItem mi in _menuItems)
                    {
                        mi.IsChecked = (string)mi.Tag == typeName;
                    }
                }
            }
        }
    }
}
