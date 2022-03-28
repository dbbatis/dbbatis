using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using DBBatis.Security;
using System.Text;
using System.Xml;

namespace DBBatis.Action
{

    /// <summary>
    /// 检查表中唯一键委托
    /// </summary>
    /// <param name="tableName"></param>
    /// <param name="pkField"></param>
    /// <param name="field"></param>
    /// <param name="isAdd"></param>
    /// <returns></returns>
    public delegate string GetCheckUniqueKeySQL(Page p, string field, bool isAdd);
    /// <summary>
    /// Action Page
    /// </summary>
    public class Page
    {
        
        /// <summary>
        /// ID
        /// </summary>
        public short ID { get; set; }
        /// <summary>
        /// 操作数据库
        /// </summary>
        public string DbFlag { get; set; }
        /// <summary>
        /// 模块或表名
        /// </summary>
        public string Name { get; set; }
        /// <summary>
        /// DbConfig
        /// </summary>
        public DbConfig DbConfig { get; internal set; }
        /// <summary>
        /// 是否支持工作流
        /// </summary>
        public bool IsSupportWorkFlow { get; set; }
        /// <summary>
        /// PKField
        /// </summary>
        public string PKField { get; set; }
        /// <summary>
        /// 
        /// </summary>
        public object Tag { get; set; }
        /// <summary>
        /// 唯一键，系统在保存时会做检查
        /// </summary>
        public string[] UniqueKeys { get; set; }
        /// <summary>
        /// 唯一键检查SQL
        /// </summary>
        public string CheckUniqueKeyOtherSQL { get; set; }
        internal string InsertCheckUniqueSQL { get; set; }
        internal string UpdateCheckUniqueSQL { get; set; }
        /// <summary>
        /// DbAction集合
        /// </summary>
        public Dictionary<string, DbAction> DbActions { get; set; }
        /// <summary>
        /// BatchDbAction集合
        /// </summary>
        public Dictionary<string, BatchDbAction> BatchDbActions { get; set; }

        /// <summary>
        /// 根据文件,实体Page
        /// </summary>
        /// <param name="filePath"></param>
        /// <returns></returns>
        internal static Page InitPage(string filePath)
        {
            FileInfo f = new FileInfo(filePath);

            return InitPage(f);
        }
        
        public delegate void PageAfterInit(Page page);
        /// <summary>
        /// Page加载后处理委托
        /// </summary>
        public static PageAfterInit PageAfterInitHander { get; set; }
        /// <summary>
        /// 根据文件,实体Page
        /// </summary>
        /// <param name="file"></param>
        /// <returns></returns>
        internal static Page InitPage(FileInfo file)
        {
            System.Diagnostics.Debug.WriteLine(string.Format("Begin Load File:{0}", file.FullName));

            XmlDocument doc = new XmlDocument();
            try
            {
                doc.Load(file.FullName);
            }
            catch (Exception err)
            {
                ActionManager.ErrLog.AddLogInfo(string.Format("不是有效Page文件 File:{0}", file.FullName)
                        , err.ToString());
                return null;
            }

            if (string.IsNullOrEmpty(doc.DocumentElement.NamespaceURI)) return null;
            Page p = null;

            XmlNamespaceManager xnsm = new XmlNamespaceManager(doc.NameTable);
            xnsm.AddNamespace("ns", doc.DocumentElement.NamespaceURI);
            XmlNode tempnode = null;
            XmlNode pageNode = doc.SelectSingleNode("ns:Page", xnsm);
            if (pageNode == null)
            {
                System.Diagnostics.Debug.WriteLine(string.Format("不是有效Page文件 File:{0}", file.FullName));
            }
            else
            {
                p = new Page();
                p.Name = pageNode.Attributes["Name"].Value;
                p.PKField = pageNode.Attributes["PKField"].Value;
                p.ID = short.Parse(pageNode.Attributes["ID"].Value);

                #region //设置DbFlag
                tempnode = pageNode.SelectSingleNode("ns:DBFlag", xnsm);
                if (tempnode == null)
                {
                    p.DbFlag = MainConfig.DbConfigs.First().Key;
                }
                else
                {
                    if (MainConfig.DbConfigs.Keys.Contains(tempnode.InnerText) == false)
                    {
                        throw new ApplicationException(string.Format("文件DbFlag无效:{0}", file.FullName));
                    }
                    p.DbFlag = tempnode.InnerText;
                }
                p.DbConfig = MainConfig.DbConfigs[p.DbFlag];
                #endregion

                if (pageNode.Attributes["IsSupportWorkFlow"] != null)
                {
                    p.IsSupportWorkFlow = pageNode.Attributes["IsSupportWorkFlow"].Value.Equals("true", StringComparison.CurrentCultureIgnoreCase)
                        || pageNode.Attributes["IsSupportWorkFlow"].Value.Equals("1", StringComparison.CurrentCultureIgnoreCase);
                }

                if (pageNode.Attributes["UniqueKeys"] != null)
                {
                    if (pageNode.Attributes["CheckUniqueKeyOtherSQL"] != null)
                    {
                        p.CheckUniqueKeyOtherSQL = pageNode.Attributes["CheckUniqueKeyOtherSQL"].Value;
                        if (string.IsNullOrEmpty(p.CheckUniqueKeyOtherSQL) == false)
                        {
                            p.CheckUniqueKeyOtherSQL = p.CheckUniqueKeyOtherSQL.Trim();
                            if (p.CheckUniqueKeyOtherSQL.IndexOf("AND ", StringComparison.CurrentCultureIgnoreCase) == -1)
                            {
                                p.CheckUniqueKeyOtherSQL = string.Format(" AND {0}", p.CheckUniqueKeyOtherSQL);
                            }
                        }
                    }



                    p.UniqueKeys = pageNode.Attributes["UniqueKeys"].Value.Split(';');
                    


                    StringBuilder sbinsertcheck = new StringBuilder();
                    StringBuilder sbupdatecheck = new StringBuilder();
                    foreach (string key in p.UniqueKeys)
                    {
                        if (string.IsNullOrEmpty(key)) continue;
                        sbinsertcheck.Append(p.DbConfig.GetCheckUniqueKeySQLHandler(p, key, true));
                        sbupdatecheck.Append(p.DbConfig.GetCheckUniqueKeySQLHandler(p, key, false));
                    }
                    p.InsertCheckUniqueSQL = sbinsertcheck.ToString();
                    p.UpdateCheckUniqueSQL = sbupdatecheck.ToString();
                }

                
                //加载Action
                XmlNodeList actionnodes = doc.SelectNodes("//ns:Page/ns:DBAction", xnsm);
                p.DbActions = new Dictionary<string, DbAction>();
                foreach (XmlNode n in actionnodes)
                {
                    DbAction dbaction = DbAction.InitDbAction(n, xnsm,  p);
                    if (dbaction != null)
                    {
                        if (string.IsNullOrEmpty(dbaction.ParameterDefaultTable))
                        {
                            dbaction.ParameterDefaultTable = p.Name;
                        }

                        p.DbActions.Add(dbaction.Name, dbaction);
                    }
                }
                //加载BatchDbAction
                actionnodes = doc.SelectNodes("//ns:Page/ns:BatchDBAction", xnsm);
                p.BatchDbActions = new Dictionary<string, BatchDbAction>();
                foreach (XmlNode n in actionnodes)
                {
                    BatchDbAction dbaction = BatchDbAction.InitBatchDBAction(n, xnsm,  p);
                    if (dbaction != null)
                    {
                        p.BatchDbActions.Add(dbaction.Name, dbaction);
                    }
                }
                //检查BatchDbAction与DbAction名称不能相同
                StringBuilder sb = new StringBuilder();
                foreach (string key in p.BatchDbActions.Keys)
                {
                    if (p.DbActions.ContainsKey(key))
                    {
                        sb.AppendFormat("[{0}].Page中ActionName与BatchActionName重复。请改用其它名字.{1}", p.ID, Environment.NewLine);
                    }
                }
                if (sb.Length > 0)
                {
                    System.Diagnostics.Debug.WriteLine(sb.ToString());
                    throw new ApplicationException(sb.ToString());
                }
            }
            if (PageAfterInitHander != null)
            {
                PageAfterInitHander(p);
            }
            //if (p.IsSupportWorkFlow)
            //{
            //    //支持工作流
            //    DbAction flowadd = ActionManager.GloblaDbAction.DbActions["Flow_SetTableField"];
            //    Dictionary<string, string> nvs = new Dictionary<string, string>();
            //    nvs.Add("TableName", p.Name);
            //    HandlerValue keyValueHandler = new HandlerValue(nvs);
            //    //创建工作流表
            //    ActionResult flowresult = ActionManager.Instance().DoGlobalDbAction(flowadd.Name, keyValueHandler, 1, null, flowadd.DbConfig.ConnectionString);

            //    if (flowresult.IsOK == false)
            //    {
            //        throw new ApplicationException(flowresult.ErrMessage);
            //    }
            //}
            return p;
        }

        internal static Dictionary<int, Page> InitPage()
        {
            Dictionary<int, Page> _Pages = new Dictionary<int, Page>();
            if (string.IsNullOrEmpty(MainConfig.PagePath)) return _Pages;

            System.IO.DirectoryInfo dirinfo = new System.IO.DirectoryInfo(MainConfig.PagePath);
            FileInfo[] files = dirinfo.GetFiles("*.page", SearchOption.AllDirectories);
            foreach (FileInfo f in files)
            {

                Page p = Page.InitPage(f);
                if (p != null)
                {
                    _Pages.Add(p.ID, p);
                }
            }
            return _Pages;
        }
        /// <summary>
        /// 获取加载命令
        /// </summary>
        /// <returns></returns>
        public DbAction GetInitAction()
        {
            return GetAction("Init");
        }
        /// <summary>
        /// 根据名称获取Action
        /// </summary>
        /// <param name="actionName">区分大小写</param>
        /// <returns></returns>
        public DbAction GetAction(string actionName)
        {
            return GetAction(actionName, null);
        }

        /// <summary>
        /// 根据名称获取Action
        /// </summary>
        /// <param name="actionName">区分大小写</param>
        /// <returns></returns>
        public DbAction GetAction(string actionName,ActionData handlerValue)
        {
            if (string.IsNullOrEmpty(actionName) == false)
            {
                if (this.DbActions.ContainsKey(actionName))
                {
                    return this.DbActions[actionName];
                }
                else
                {
                    if (actionName.Equals("Save"))
                    {
                        if(handlerValue == null)
                        {
                            throw new ArgumentNullException("请指定handlerValue");
                        }
                        if(handlerValue[this.PKField] == "")
                        {
                            return this.DbActions["Insert"];
                        }
                        else
                        {
                            return this.DbActions["Update"];
                        }
                    }
                    throw new ApplicationException(string.Format("未检查到Action【{0}】", actionName));
                }
            }
            else
            {
                throw new ApplicationException("ActionName不能为空");
            }

        }
    }
}
