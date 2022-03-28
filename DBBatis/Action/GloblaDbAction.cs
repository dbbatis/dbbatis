using System;
using System.Collections.Generic;
using System.Data;
using System.Data.Common;
using System.IO;
using System.Linq;
using System.Xml;

namespace DBBatis.Action
{
    public class GloblaDbAction
    {
        /// <summary>
        /// 操作数据库
        /// </summary>
        public string DbFlag { get; set; }
        /// <summary>
        /// DbAction集合
        /// </summary>
        public System.Collections.Generic.Dictionary<string, DbAction> DbActions { get; set; }



        internal static GloblaDbAction InitGlobalDbAction(string path)
        {
            GloblaDbAction GloblaDbAction = new GloblaDbAction();
            GloblaDbAction.DbActions = new Dictionary<string, DbAction>();

            if (string.IsNullOrEmpty(path)) return GloblaDbAction;
            lock (ComboBoxSQL)
            {
                ComboBoxSQL.Clear();
            }
            System.IO.DirectoryInfo dirinfo = new System.IO.DirectoryInfo(path);
            FileInfo[] files = dirinfo.GetFiles("*.action", SearchOption.AllDirectories);
            foreach (FileInfo f in files)
            {
                InitGlobalDbAction(GloblaDbAction,f);
            }
            return GloblaDbAction;
        }
        private static DataTable _ComboBoxSQL = new DataTable();
        /// <summary>
        /// 全局XML中的ComboBoxSQL
        /// </summary>
        internal static DataTable ComboBoxSQL
        {
            get
            {
                lock (_ComboBoxSQL)
                {
                    if (_ComboBoxSQL.Columns.Count == 0)
                    {
                        _ComboBoxSQL.Columns.Add("ObjectName", typeof(string));
                        _ComboBoxSQL.Columns.Add("SelectSQL", typeof(string));
                        _ComboBoxSQL.Columns.Add("LikeSQL", typeof(string));
                        _ComboBoxSQL.Columns.Add("OrderBy", typeof(string));
                        _ComboBoxSQL.Columns.Add("TableName", typeof(string));
                        _ComboBoxSQL.Columns.Add("IsNeedLogin", typeof(bool));
                        _ComboBoxSQL.Columns["IsNeedLogin"].DefaultValue = true;
                    }
                }
                return _ComboBoxSQL;
            }
        }

        private static void AddComboBoxSQL(string objectName, string selectSql, string likeSQL, string orderBySql, string tableName, bool isneedlogin)
        {
            lock (ComboBoxSQL)
            {
                if (ComboBoxSQL.Select(string.Format("ObjectName='{0}'", objectName)).Length == 0)
                {
                    DataRow row = ComboBoxSQL.NewRow();
                    row["ObjectName"] = objectName;
                    row["SelectSQL"] = selectSql;
                    row["LikeSQL"] = likeSQL;
                    row["OrderBy"] = orderBySql;
                    row["TableName"] = tableName;
                    row["IsNeedLogin"] = isneedlogin;
                    ComboBoxSQL.Rows.Add(row);
                }
            }
        }
        #region//下拉相关
        /// <summary>
        /// 获取内存下拉SQL
        /// </summary>
        /// <param name="objectName"></param>
        /// <param name="language"></param>
        /// <param name="userid"></param>
        /// <returns></returns>
        public static string GetComboBoxSQL(string objectName, string language, int userid)
        {
            return GetComboBoxSQL(objectName, language, userid, true);
        }
        /// <summary>
        /// 获取内存下拉SQL
        /// </summary>
        /// <param name="objectName"></param>
        /// <param name="language"></param>
        /// <param name="session"></param>
        /// <returns></returns>
        public static string GetComboBoxSQL(string objectName, string language, int userid, bool needLogin)
        {
            string tablename = objectName;
            string viewid = string.Empty;
            if (needLogin)
            {
                if (BaseUser.GetViewBaseID != null)
                {
                    viewid = BaseUser.GetViewBaseID(userid, tablename);
                    if (!string.IsNullOrEmpty(viewid))
                    {
                        viewid = string.Format(" AND ID IN({0})", viewid);
                    }
                }
            }

            DataRow row = GetComboBoxSQLRow(objectName, language, string.Empty);



            string setting = string.Format("{0} {1} {2}", row["SelectSQL"], viewid, row["OrderBY"]);
            return setting;
        }
        private static DataRow GetComboBoxSQLRow(string objectName, string language, string orderBySql)
        {
            lock (ComboBoxSQL)
            {
                DataRow[] rows = ComboBoxSQL.Select(string.Format("ObjectName='{0}'", objectName));
                if (rows.Length == 0)
                {
                    throw new ApplicationException(string.Format("请指定【{0}】下拉配置", objectName));
                }
                else
                {
                    rows[0]["SelectSQL"] = string.Format(rows[0]["SelectSQL"].ToString(), language);
                    rows[0]["LikeSQL"] = string.Format(rows[0]["LikeSQL"].ToString(), language);
                }
                return rows[0];
            }
        }


        /// <summary>
        /// 获取内存下拉框配置语句
        /// </summary>
        /// <param name="objectName">表名</param>
        /// <param name="language">语言,默认为空、英文为e,根据当前用户语言类型来传入</param>
        /// <param name="pid">父ID,没有则为空</param>
        /// <param name="session">当前对话</param>
        /// <returns></returns>
        public static DbCommand GetComboBoxSQL(string objectName, string language, string pid, int userid, bool needLogin)
        {

            string setting = GetComboBoxSQL(objectName, language, userid, needLogin);
            DbCommand cmmd = MainConfig.GetDbConfig().Connection.CreateCommand();
            cmmd.CommandText = setting;
            if (string.IsNullOrEmpty(pid) == false)
            {
                //添加关联PID参数
                DbParameter p = cmmd.CreateParameter();
                p.ParameterName = "PID";
                p.Value = pid;
                cmmd.Parameters.Add(p);
            }
            //添加参数
            MappingBind.AddUserParameter(cmmd, userid);
            MappingBind.AddParameterHandler?.Invoke(cmmd, userid);
            return cmmd;
        }
        /// <summary>
        /// 获取实时下拉框配置语句
        /// </summary>
        /// <param name="objectName">JS调用名</param>
        /// <param name="language">语言,默认为空、英文为e,根据当前用户语言类型来传入</param>
        /// <param name="pid">父ID,没有则为空</param>
        /// <param name="value">匹配值</param>
        /// <param name="session">当前对话</param>
        /// <returns></returns>
        public static DbCommand GetAutocompleteSQL(string objectName, string language, string pid, string value, int userid)
        {
            return GetAutocompleteSQLSetting(objectName, language, pid, value, userid, true);
        }
        /// <summary>
        /// 获取实时下拉框配置语句
        /// </summary>
        /// <param name="objectName">JS调用名</param>
        /// <param name="language">语言,默认为空、英文为e,根据当前用户语言类型来传入</param>
        /// <param name="pid">父ID,没有则为空</param>
        /// <param name="value">匹配值</param>
        /// <param name="session">当前对话</param>
        /// <param name="needLogin">是否需要登录</param>
        /// <returns></returns>
        public static DbCommand GetAutocompleteSQLSetting(string objectName, string language, string pid, string value,
            int userid, bool needLogin)
        {
            string viewid = string.Empty;
            if (needLogin)
            {
                if (BaseUser.GetViewBaseID != null)
                {
                    viewid = BaseUser.GetViewBaseID(userid, objectName);
                    if (!string.IsNullOrEmpty(viewid))
                    {
                        viewid = string.Format(" AND ID IN({0})", viewid);
                    }
                }

            }

            objectName = string.Format("{0}{1}", objectName, language);

            DataRow row = GetComboBoxSQLRow(objectName, language, "ORDER BY CODE");

            DbCommand cmmd = MainConfig.GetDbConfig().Connection.CreateCommand();
            string setting = string.Format("{0} {1} {2} {3}", row["SelectSQL"], viewid, row["LikeSQL"], row["OrderBY"]);
            cmmd.CommandText = setting;
            if (string.IsNullOrEmpty(pid) == false)
            {
                //添加关联PID参数
                DbParameter p = cmmd.CreateParameter();
                p.ParameterName = "PID";
                p.Value = pid;
                cmmd.Parameters.Add(p);
            }
            //添加@V参数
            DbParameter pv = cmmd.CreateParameter();
            pv.ParameterName = "V";
            pv.Value = string.Format("%{0}%", value);
            cmmd.Parameters.Add(pv);
            //添加参数
            MappingBind.AddUserParameter(cmmd, userid);
            MappingBind.AddParameterHandler?.Invoke(cmmd, userid);
            return cmmd;
        }
        #endregion

        private static void InitGlobalDbAction(GloblaDbAction GloblaDbAction,FileInfo file)
        {
            System.Diagnostics.Debug.WriteLine(string.Format("Begin Load File:{0}", file.FullName));

            XmlDocument doc = new XmlDocument();
            try
            {
                doc.Load(file.FullName);
            }
            catch (Exception err)
            {
                ActionManager.ErrLog.AddLogInfo(string.Format("不是有效XML文件 File:{0}", file.FullName)
                        , err.ToString());
                System.Diagnostics.Debug.WriteLine(string.Format("不是有效XML文件 File:{0}", file.FullName));

            }
            

            if (string.IsNullOrEmpty(doc.DocumentElement.NamespaceURI)) return;


            XmlNamespaceManager xnsm = new XmlNamespaceManager(doc.NameTable);
            xnsm.AddNamespace("ns", doc.DocumentElement.NamespaceURI);
            
            XmlNode globalNode = doc.SelectSingleNode("ns:Global", xnsm);
            //是否设置DbFlag
            XmlNode tempnode = globalNode.SelectSingleNode("ns:DBFlag", xnsm);
            if (tempnode == null)
            {
                GloblaDbAction.DbFlag = MainConfig.DbConfigs.First().Key;
            }
            else
            {
                string tempdbflag = tempnode.InnerText;
                if (MainConfig.DbConfigs.Keys.Contains(tempdbflag) == false)
                {
                    throw new ApplicationException(string.Format("DbFlag【{0}】无效，文件:【{1}】",tempdbflag, file.FullName));
                }
                GloblaDbAction.DbFlag = tempdbflag;
            }
            XmlNode actionNode = globalNode.SelectSingleNode("ns:DBActions", xnsm);
            if (actionNode == null)
            {
                System.Diagnostics.Debug.WriteLine(string.Format("不是有效DBAction文件 File:{0}", file.FullName));
            }
            XmlNodeList actionnodes = actionNode.SelectNodes("ns:DBAction", xnsm);
            if (actionnodes != null)
            {


                
                DbConfig cnn = MainConfig.DbConfigs[GloblaDbAction.DbFlag];
                foreach (XmlNode n in actionnodes)
                {
                    DbAction dbaction = DbAction.InitDbAction(n, xnsm, cnn);
                    if (dbaction != null)
                    {
                        if (GloblaDbAction.DbActions.ContainsKey(dbaction.Name))
                        {
                            throw new ApplicationException(string.Format("Global.DBAction:{0}已经存在", dbaction.Name));
                        }
                        else
                        {
                            GloblaDbAction.DbActions.Add(dbaction.Name, dbaction);
                        }

                    }
                }
            }
            XmlNode comboBoxsqlsnode = globalNode.SelectSingleNode("ns:ComboBoxSQLs",xnsm);
            if(comboBoxsqlsnode != null)
            {
                XmlNodeList comboBoxsqlnodes = comboBoxsqlsnode.SelectNodes("ns:ComboBoxSQL",xnsm);
                foreach(XmlNode n in comboBoxsqlnodes)
                {
                    string name = n.Attributes["Name"].Value;
                    string tablename = string.Empty;
                    string orderby = string.Empty;
                    bool isneedlogin = true;
                    if (n.Attributes["TableName"]!= null)
                    {
                        tablename = n.Attributes["TableName"].Value;
                    }

                    if(n.Attributes["OrderBy"] != null)
                    {
                        orderby = n.Attributes["OrderBy"].Value.Trim();
                    }
                    if (n.Attributes["NeedLogin"] != null)
                    {
                        isneedlogin = actionNode.Attributes["NeedLogin"].Value.Equals("true", StringComparison.CurrentCultureIgnoreCase)
                            || actionNode.Attributes["NeedLogin"].Value.Equals("1", StringComparison.CurrentCultureIgnoreCase);
                    }
                    else
                    {
                        //默认需要登录才可执行
                        isneedlogin = true;
                    }

                    string sql = n.SelectSingleNode("ns:SelectSQL",xnsm).InnerText;
                    string likesql = string.Empty;
                    tempnode = n.SelectSingleNode("ns:LikeSQL",xnsm);
                    if (tempnode != null)
                    {
                        likesql = tempnode.InnerText.Trim();
                    }
                    
                    AddComboBoxSQL(name, sql, likesql, orderby, tablename,isneedlogin);


                }
            }
            doc = null;
        }

    }
}
