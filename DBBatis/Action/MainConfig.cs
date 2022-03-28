using System;
using System.Collections.Generic;
using System.Xml;
using System.IO;
using DBBatis.Security;
using System.Collections.Specialized;
using System.Reflection;

namespace DBBatis.Action
{

    /// <summary>
    /// 主配置文件
    /// </summary>
    public class MainConfig
    {
        internal static bool IsNeedUpdate = false;
        internal static Dictionary<string, DbConfig> DbConfigs = new Dictionary<string, DbConfig>();
        internal static System.Collections.Specialized.StringCollection UrlKeys = null;
        /// <summary>
        /// 加密Key
        /// </summary>
        internal static string EncryptionKey { get; set; }
        /// <summary>
        /// 是否加密
        /// </summary>
        internal static bool IsEncryption { get; set; }
        internal static string ActionHandlerPath{get;set;}
        internal static StringCollection ActionHandlerFiles;
        public static string PagePath
        {
            get;
            internal set;
        }
        static string _MainConfigPath;
        static FileSystemWatcher fileSystemWatcher = new FileSystemWatcher();
        static FileSystemWatcher ActionHandlerWatcher= new FileSystemWatcher(); 
        
        /// <summary>
        /// 主配置文件路径
        /// </summary>
        public static string MainConfigPath { get
            {
                if (string.IsNullOrEmpty(_MainConfigPath))
                {
                    string path = AppDomain.CurrentDomain.BaseDirectory;
                    System.IO.DirectoryInfo f = new System.IO.DirectoryInfo(path);
                    while (true)
                    {
                        
                        string file = string.Format(@"{0}\\XML\MainConfig.xml", path);
                        if (System.IO.File.Exists(file))
                        {
                            _MainConfigPath = file;
                            break;
                        }
                        if(f.Parent != null)
                        {
                            path = f.Parent.FullName;
                            f = new DirectoryInfo(path);
                        }
                        else
                        {
                            break;
                        }
                    }
                }
                lock (fileSystemWatcher)
                {
                    if (string.IsNullOrEmpty(fileSystemWatcher.Path) && !string.IsNullOrEmpty(_MainConfigPath))
                    {
                        string watcherpath = Path.GetDirectoryName(_MainConfigPath);
                        fileSystemWatcher = new FileSystemWatcher(watcherpath, "*.*");
                        fileSystemWatcher.IncludeSubdirectories = true;
                        fileSystemWatcher.Changed += FileSystemWatcher_Changed;
                        fileSystemWatcher.Renamed += FileSystemWatcher_Renamed;
                        fileSystemWatcher.EnableRaisingEvents = true;
                    }
                }
                return _MainConfigPath;
            } 
            set
            {
                _MainConfigPath = value;
                if (!string.IsNullOrEmpty(value))
                {
                    ActionManager.Instance();
                }
            }
        }

        private static void FileSystemWatcher_Renamed(object sender, RenamedEventArgs e)
        {
            var watcher = sender as FileSystemWatcher;
            XMLChangeUpdate(e.Name, e.FullPath);
        }

        private static void FileSystemWatcher_Changed(object sender, FileSystemEventArgs e)
        {
            var watcher = sender as FileSystemWatcher;
            XMLChangeUpdate(e.Name, e.FullPath);
        }

        private static void XMLChangeUpdate(string name, string fullPath)
        {
            if (name.Equals("MainConfig.xml", StringComparison.OrdinalIgnoreCase))
            {
                IsNeedUpdate = true;
            }
            else if (name.EndsWith(".Page", StringComparison.OrdinalIgnoreCase))
            {
                string[] tempnames = name.Split('\\');
                string pageidname = tempnames[tempnames.Length - 1].Split('.')[0];
                int pageid = 0;
                if (int.TryParse(pageidname, out pageid))
                {
                    ActionManager.RemovePage(pageid);
                }
            }
            else if (name.EndsWith(".Action", StringComparison.OrdinalIgnoreCase))
            {
                ActionManager.GloblaDbAction = GloblaDbAction.InitGlobalDbAction(MainConfig.GlobalDbActionPath);
            }
        }
        /// <summary>
        /// 获取默认配置信息
        /// </summary>
        /// <returns></returns>
        public static DbConfig GetDbConfig()
        {
            return GetDbConfig(string.Empty);
        }
        /// <summary>
        /// 获取指定DB
        /// </summary>
        /// <param name="dbFlag"></param>
        /// <returns></returns>
        public static DbConfig GetDbConfig(string dbFlag)
        {
            if (DbConfigs == null || DbConfigs.Count==0)
            {
                //检查是否指定XML配置文件
                if(string.IsNullOrEmpty(MainConfigPath))
                {
                    throw new ApplicationException("请指定MainConfig.MainConfigPath属性。");
                }
                ActionManager.Instance();
            }
            if (string.IsNullOrEmpty(dbFlag))
            {
                if (DbConfigs.Count > 0)
                {
                    foreach (string key in DbConfigs.Keys)
                    {
                        return DbConfigs[key];
                    }
                }
                throw new ApplicationException("未找到DB配置信息，请设置DB配置。");
            }
            else if (DbConfigs.ContainsKey(dbFlag))
                return DbConfigs[dbFlag];
            else
            {
                throw new ApplicationException(string.Format("主配置文件中，未找到【{0}】DB配置。", dbFlag));
            }
        }
        public static string GlobalDbActionPath
        {
            get;
            internal set;
        }

        internal static Dictionary<short, Dictionary<string, ActionHandlerBase>> PageHandlers = new Dictionary<short, Dictionary<string, ActionHandlerBase>>();
        internal static Dictionary<string, ActionHandlerBase> GlobalHanlers = new Dictionary<string, ActionHandlerBase>();
        internal static void InitActionHandler(string file)
        {
            byte[] bytes;
            using (FileStream stream = System.IO.File.Open(file, FileMode.Open, FileAccess.Read, FileShare.Read))
            {
                bytes=new byte[stream.Length];
                stream.Read(bytes, 0, bytes.Length);
            }

            Assembly assembly = Assembly.Load(bytes);
            Type[] array2 = assembly.GetTypes();
            for (int k = 0; k < array2.Length; k++)
            {
                Type T2 = array2[k];
                if (T2.BaseType == typeof(ActionHandlerBase))
                {
                    Type[] types = new Type[0];

                    ActionHandlerBase service = (ActionHandlerBase)T2.GetConstructor(types)
                        .Invoke(new string[] { });
                    if (service.PageID != 0)
                    {
                        if (PageHandlers.ContainsKey(service.PageID))
                        {
                            PageHandlers[service.PageID].Add(service.ActionName, service);
                        }
                        else
                        {
                            PageHandlers.Add(service.PageID, new Dictionary<string, ActionHandlerBase>()
                                        {
                                            {service.ActionName, service},
                                        });
                        }
                    }
                    else
                    {
                        GlobalHanlers.Add(service.ActionName, service);
                    }
                }
            }
        }
        /// <summary>
        /// 初始化数据库连接信息
        /// </summary>
        internal static void InitMainConfig(string configPath)
        {

            System.Diagnostics.Debug.Assert(!string.IsNullOrEmpty(configPath), "请指定MainConfigPath");

            string path = configPath;
            if (File.Exists(path) == false)
            {
                Directory.SetCurrentDirectory(AppDomain.CurrentDomain.BaseDirectory);//设置当前路径，以便使用相对路径
                if (File.Exists(path) == false)
                    return;
            }

            XmlDocument doc = new XmlDocument();
            doc.Load(path);
            XmlNamespaceManager xnsm = new XmlNamespaceManager(doc.NameTable);
            xnsm.AddNamespace("ns", doc.DocumentElement.NamespaceURI);
            //设置PagePath
            //设置路径
            string xmlpath = System.IO.Path.GetDirectoryName(path);
            string currentdirectory_old = Directory.GetCurrentDirectory();
            Directory.SetCurrentDirectory(xmlpath);
            XmlNode tempnode = doc.SelectSingleNode("//ns:PagePath", xnsm);
            if (tempnode != null)
            {
                PagePath = Path.GetFullPath(tempnode.InnerText);
            }

            tempnode = doc.SelectSingleNode("//ns:GlobalDbActionPath", xnsm);
            if (tempnode != null)
            {
                GlobalDbActionPath = Path.GetFullPath(tempnode.InnerText);
            }
            tempnode = doc.SelectSingleNode("//ns:ActionHandler", xnsm);
            if (tempnode != null)
            {
                ActionHandlerPath = Path.GetFullPath(tempnode.Attributes["Path"].InnerText);
                XmlNodeList nodes = tempnode.SelectNodes("ns:File",xnsm);

                lock (ActionHandlerWatcher)
                {
                    if (string.IsNullOrEmpty(ActionHandlerWatcher.Path))
                    {
                        
                        ActionHandlerWatcher = new FileSystemWatcher(ActionHandlerPath, "*.dll");
                        ActionHandlerWatcher.Changed += ActionHandlerWatcher_Changed;
                        ActionHandlerWatcher.Renamed += ActionHandlerWatcher_Changed;
                    }
                }
                ActionHandlerFiles = new StringCollection();
                GlobalHanlers = new Dictionary<string, ActionHandlerBase>();
                PageHandlers = new Dictionary<short, Dictionary<string, ActionHandlerBase>>();
                foreach (XmlNode n in nodes)
                {
                  string file =string.Format("{0}\\{1}",ActionHandlerPath,n.InnerText);
                    if (File.Exists(file))
                    {
                        ActionHandlerFiles.Add(file);
                        InitActionHandler(file);
                    }
                    else
                    {
                        throw new FileNotFoundException(file);
                    }
                }
            }
            //设置回原来路径
            Directory.SetCurrentDirectory(currentdirectory_old);

            tempnode = doc.SelectSingleNode("//ns:Encryption", xnsm);
            if (tempnode != null)
            {
                EncryptionKey = tempnode.Attributes["Key"].InnerText;
                IsEncryption = tempnode.Attributes["IsEncryption"].InnerText == "1"
                    || tempnode.Attributes["IsEncryption"].InnerText == "true";
            }
            bool needsavedoc = false;
            XmlNodeList nodelist = doc.SelectNodes("//ns:DB", xnsm);
            foreach (XmlNode n in nodelist)
            {
                XmlNode ntype = n.SelectSingleNode("ns:Type", xnsm);
                string type = "";
                if (ntype != null)
                {
                    type = ntype.InnerText;
                }
                XmlNode tempNode = n.SelectSingleNode("ns:ConnectionString", xnsm);
                DbConfig cnn = null;
                bool cnnisencry = CheckEncryptOrDecrypt(tempNode.InnerText);
                if (IsEncryption)
                {
                        if (cnnisencry == false)
                        {
                            tempNode.InnerText = EncryptOrDecrypt(tempNode.InnerText, false);
                            needsavedoc = true;
                        }

                    
                    cnn = DbConfig.GetDbConfig(type, EncryptOrDecrypt(tempNode.InnerText, true));
                }
                else
                {
                    
                        if (cnnisencry)
                        {
                            tempNode.InnerText = EncryptOrDecrypt(tempNode.InnerText, true);
                            needsavedoc = true;

                        }

                    
                    cnn = DbConfig.GetDbConfig(type, tempNode.InnerText);
                }


                cnn.DBFlag = n.Attributes["DBFlag"].InnerText;
                lock (DbConfigs)
                {
                    cnn.IsLog = !IsEncryption;
                    if (DbConfigs.ContainsKey(cnn.DBFlag))
                        DbConfigs[cnn.DBFlag] = cnn;
                    else
                        DbConfigs.Add(cnn.DBFlag, cnn);
                }

            }
            nodelist = doc.SelectNodes("//ns:UrlKey", xnsm);
            UrlKeys = new System.Collections.Specialized.StringCollection();
            foreach (XmlNode n in nodelist)
            {


                XmlNode nvalue = n.SelectSingleNode("ns:Value", xnsm);
                string keyvalue = "";
                if (nvalue != null)
                {
                    keyvalue = nvalue.InnerText;
                    UrlKeys.Add(keyvalue);
                }
            }

            if (needsavedoc)
            {
                SetFileCanWirte(path);
                doc.Save(path);
            }
        }

        private static void ActionHandlerWatcher_Changed(object sender, FileSystemEventArgs e)
        {
            MainConfig.IsNeedUpdate = true;
        }

        private static void SetFileCanWirte(string file)
        {
            if (System.IO.File.Exists(file))
            {
                FileInfo f = new FileInfo(file);
                if((f.Attributes&FileAttributes.ReadOnly)== FileAttributes.ReadOnly)
                {
                    f.Attributes &= ~FileAttributes.ReadOnly;
                }
            }
        }
        /// <summary>
        /// 字符串是否加密
        /// </summary>
        /// <param name="value"></param>
        /// <returns></returns>
        internal static bool CheckEncryptOrDecrypt(string value)
        {
            if (!(string.IsNullOrEmpty(value) || value.Length < 2))
            {
                value = value.Substring(2);
                try
                {
                    value = Security.MD5.DecryptByDefaultKey(value);
                    return true;
                }
                catch (Exception err)
                {
                    ActionManager.ErrLog.AddLogInfo(String.Format("检查是否加密:{0}", value)
                        , err.ToString());
                }

            }
            return false;
        }
        /// <summary>
        /// 加解密
        /// </summary>
        /// <param name="value"></param>
        /// <param name="isDecrypt"></param>
        /// <returns></returns>
        internal static string EncryptOrDecrypt(string value, bool isDecrypt)
        {
            if (isDecrypt)
            {
                if (!(string.IsNullOrEmpty(value) || value.Length < 2))
                {
                    value = value.Substring(2);
                    value = MD5.DecryptByDefaultKey(value);
                }
            }
            else
            {
                value = string.Format("=={0}", MD5.EncryptByDefaultKey(value));
            }
            return value;
        }
    }
}
