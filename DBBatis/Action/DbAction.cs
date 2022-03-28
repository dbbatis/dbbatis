using System;
using System.Collections.Generic;
using System.Text;
using System.Data;
using System.Data.Common;
using System.Collections.Specialized;
using System.Xml;
using System.Text.RegularExpressions;
using DBBatis.IO.Log;
using DBBatis.Web;
using System.Collections;
using System.Collections.ObjectModel;
using System.Threading;

namespace DBBatis.Action
{
    public delegate void LogCommandHandler(CommandInfo commandInfo);
    /// <summary>
    /// 命令信息
    /// </summary>
    public class CommandInfo
    {
        public double TotalSeconds { get; set; }
        public string ActionName { get; set; }
        public string DbCommand { get; set; }
    }
    /// <summary>
    /// DbAction
    /// </summary>
    public class DbAction
    {
        static byte _TimeOutLog = 3;
        /// <summary>
        /// 执行命令时超过多少秒时，记录到日志中
        /// </summary>
        public static byte TimeOutLog
        {
            get
            {
                return _TimeOutLog;
            }
            set
            {
                _TimeOutLog = value;
            }
        }
        /// <summary>
        /// 记录超过指定时间的命令
        /// </summary>
        public static LogCommandHandler LogOutCommand { get; set; }
        ///// <summary>
        ///// 处理流程
        ///// </summary>
        //public static GetHandlerAction GetHandlerAction { get; set; }
        /// <summary>
        /// 构造函数
        /// </summary>
        public DbAction()
        {
            ActionCommand = new ActionCommand();
        }
        internal ActionHandlerBase Handler { get; set; }
        internal ActionHandlerBase GlobalHandler { get; set; }
        /// <summary>
        /// 所属Page
        /// </summary>
        public Page Page { get; internal set; }
        /// <summary>
        /// 描述
        /// </summary>
        public string Description { get; set; }
        /// <summary>
        /// 名称
        /// </summary>
        public string Name { get; set; }
        /// <summary>
        /// 是否为插入命令
        /// </summary>
        public bool IsInsert { get; internal set; }
        /// <summary>
        /// 是否为更新命令
        /// </summary>
        public bool IsUpdate { get; internal set; }
        /// <summary>
        /// 是否为初始命令
        /// </summary>
        public bool IsInit { get; private set; }
        /// <summary>
        /// 是否为全局命令
        /// </summary>
        public bool IsGlobal
        {
            get
            {
                return this.Page == null;
            }
        }
        /// <summary>
        /// 对应的FunctionID,假如指定，系统获取验证权限，前提需要BILLID
        /// </summary>
        public UInt16 FunctionID { get; set; }
        /// <summary>
        /// 是否需要登录
        /// </summary>
        public bool NeedLogin { get; set; }
        /// <summary>
        /// 检查命令
        /// </summary>
        public string CheckAction { get; set; }
        /// <summary>
        /// 参数默认对应的表
        /// </summary>
        public string ParameterDefaultTable { get; set; }
        /// <summary>
        /// 状态操作信息
        /// </summary>
        public StateOperation StateOperation { get; set; }
        /// <summary>
        /// 排序SQL
        /// </summary>
        public string OrderBy { get; set; }

        public ActionCommand ActionCommand { get; set; }
        /// <summary>
        /// 是否需要开启事务，默认开启
        /// </summary>
        public bool NeedTransaction { get; set; }
        /// <summary>
        /// 是否支持批量操作
        /// </summary>
        public bool IsSupportBatch { get; set; }
        
        /// <summary>
        /// 查询是否支持将条件Format到命令中
        /// </summary>
        public bool IsWhereFormat { get; set; }
        
        /// <summary>
        /// 是否为查询Action
        /// </summary>
        public bool IsSearchAction { get; set; }
        /// <summary>
        /// 查询参数ShortName
        /// </summary>
        public Dictionary<string, string> SearchParameterShortNames { get; set; }
        /// <summary>
        /// 查询参数Muilt
        /// </summary>
        public StringCollection SearchParameterMuilts { get; set; }
        
        /// <summary>
        /// 链接信息
        /// </summary>
        public DbConfig DbConfig { get; set; }

        /// <summary>
        /// 命令
        /// </summary>
        //public DbDataAdapter DataAdapter { get; set; }

        /// <summary>
        /// 返回信息
        /// </summary>
        public ActionResultType Result { get; set; }
        /// <summary>
        /// 是否以Where 1=1 结尾
        /// </summary>
        internal bool IsEndWithWhereSQL { get; set; }

        public DataTable DefaultTableSchema
        {
            get
            {
                return GetTableSchema(this, this.ParameterDefaultTable);
            }
        }
        /// <summary>
        /// 表结构缓存表
        /// </summary>
        private static Dictionary<string, DataTable> _TableSchemaCache = new Dictionary<string, DataTable>();
        /// <summary>
        /// 根据连接字符串，获取数据库名称
        /// </summary>
        /// <param name="connectionString"></param>
        /// <returns></returns>
        private static string GetDBName(string connectionString)
        {
            string[] keys = connectionString.Split(';');
            foreach (string key in keys)
            {
                if (key.IndexOf("database") >= 0)
                {
                    return key.Split('=')[1].Trim();
                }
            }
            return string.Empty;
        }
        /// <summary>
        /// 获取表结构
        /// </summary>
        /// <param name="cnn"></param>
        /// <param name="tableName"></param>
        /// <returns></returns>
        public static DataTable GetTableSchema(DbConfig cnn, string tableName)
        {
            lock (_TableSchemaCache)
            {
                if (string.IsNullOrEmpty(tableName))
                    throw new ApplicationException("TableName不能为空。");
                bool isdebug = false;
#if (DEBUG)
                isdebug = true;
#endif
                if (!isdebug && _TableSchemaCache.ContainsKey(tableName))
                    return _TableSchemaCache[tableName];

                
                DataTable dt = cnn.GetTableSchema(tableName);
                
                if (!isdebug) _TableSchemaCache.Add(tableName, dt);

                return dt;
            }

        }
        /// <summary>
        /// 获取表结构
        /// </summary>
        /// <param name="action"></param>
        /// <param name="tableName"></param>
        /// <returns></returns>
        public static DataTable GetTableSchema(DbAction action, string tableName)
        {
            if (string.IsNullOrEmpty(tableName)) return null;
            return GetTableSchema(action.DbConfig, tableName);
        }

        #region//初始Action
        /// <summary>
        /// 根据节点，实例化DbAction
        /// </summary>
        /// <param name="actionNode"></param>
        /// <returns></returns>
        internal static DbAction InitDbAction(XmlNode actionNode, XmlNamespaceManager xnsm,DbConfig dbConfig)
        {
            DbAction action = new DbAction();
            action.DbConfig = dbConfig;
            action.Name = actionNode.Attributes["Name"].Value;

            action.IsInit = action.Name.Equals("Init", StringComparison.CurrentCultureIgnoreCase);
            action.IsInsert = action.Name.Equals("Insert", StringComparison.CurrentCultureIgnoreCase);
            action.IsUpdate = action.Name.Equals("Update", StringComparison.CurrentCultureIgnoreCase);


            if (action.IsInit)
            {
                action.Name = "Init";
            }
            else if (action.IsInsert)
            {
                action.Name = "Insert";
            }
            else if (action.IsUpdate)
            {
                action.Name = "Update";
            }

            if (actionNode.Attributes["ParameterDefaultTable"] != null)
            {
                action.ParameterDefaultTable = actionNode.Attributes["ParameterDefaultTable"].Value;
            }
            //else
            //{
            //    if (p != null)
            //        action.ParameterDefaultTable = p.Name;
            //    else
            //        action.ParameterDefaultTable = null;
            //}

            if (actionNode.Attributes["FunctionID"] != null)
            {
                action.FunctionID = UInt16.Parse(actionNode.Attributes["FunctionID"].Value);
            }
            else
            {
                action.FunctionID = 0;
            }
            if (actionNode.Attributes["NeedLogin"] != null)
            {
                action.NeedLogin = actionNode.Attributes["NeedLogin"].Value.Equals("true", StringComparison.CurrentCultureIgnoreCase)
                    || actionNode.Attributes["NeedLogin"].Value.Equals("1", StringComparison.CurrentCultureIgnoreCase);
            }
            else
            {
                //默认需要登录才可执行
                action.NeedLogin = true;
            }





            if (actionNode.Attributes["CheckAction"] != null)
            {
                action.CheckAction = actionNode.Attributes["CheckAction"].Value;
            }
            else
            {
                action.CheckAction = string.Empty;
            }

            XmlNode descriptionNode = actionNode.SelectSingleNode("ns:Description", xnsm);
            if (descriptionNode != null)
            {
                action.Description = descriptionNode.InnerText;
            }

            //检查是否为状态操作命令
            XmlNode statenode = actionNode.SelectSingleNode("ns:StateOperation", xnsm);
            if (statenode != null)
            {
                action.StateOperation = new StateOperation();
                action.StateOperation.Type = (StateOperationType)Enum.Parse(typeof(StateOperationType), statenode.Attributes["Type"].Value);
                if (action.StateOperation.Type == StateOperationType.Do
                            || action.StateOperation.Type == StateOperationType.UnDo)
                {
                    if (statenode.Attributes["Field"] == null || string.IsNullOrEmpty(statenode.Attributes["Field"].Value))
                    {
                        throw new ApplicationException(string.Format("Action【{0}】当命令为Do或Undo时，必须指定Field。", action.Name));
                    }
                    else
                    {
                        action.StateOperation.Field = statenode.Attributes["Field"].Value;

                    }
                    if (statenode.Attributes["UnDoIsNull"] == null || string.IsNullOrEmpty(statenode.Attributes["UnDoIsNull"].Value))
                    {
                        action.StateOperation.UnDoIsNull = false;
                    }
                    else
                    {
                        if (statenode.Attributes["UnDoIsNull"].Value == "1" || statenode.Attributes["UnDoIsNull"].Value == "true")
                            action.StateOperation.UnDoIsNull = true;
                        else
                            action.StateOperation.UnDoIsNull = false;
                    }

                }

                XmlNode wherenode = statenode.SelectSingleNode("ns:WhereSQL", xnsm);
                if (wherenode != null)
                {
                    action.StateOperation.WhereSQL = wherenode.InnerText;
                }
                XmlNode statechecknode = statenode.SelectSingleNode("ns:StateCheck", xnsm);
                action.StateOperation.States = new System.Collections.ObjectModel.Collection<CheckState>();
                foreach (XmlNode n in statechecknode.ChildNodes)
                {
                    if (n.Name == "State")
                    {
                        CheckState cs = new CheckState();

                        cs.Field = n.Attributes["Field"].Value;
                        cs.Value = n.Attributes["Value"].Value;
                        cs.Lable = n.Attributes["Lable"].Value;
                        if (n.Attributes["Match"] != null && n.Attributes["Match"].Value.Trim().Length > 0)
                        {
                            cs.Match = n.Attributes["Match"].Value;
                        }
                        else
                        {
                            cs.Match = "=";
                        }
                        action.StateOperation.States.Add(cs);
                    }
                }

            }

            XmlNode orderBynode = actionNode.SelectSingleNode("ns:OrderBy", xnsm);
            if (orderBynode != null && string.IsNullOrEmpty(orderBynode.InnerText) == false)
            {
                action.OrderBy = orderBynode.InnerText.Trim();
                if (!action.OrderBy.StartsWith("ORDER BY", StringComparison.CurrentCultureIgnoreCase))
                {
                    action.OrderBy = string.Format("ORDER BY {0}", action.OrderBy);
                }
            }
            XmlNode sqlnode = actionNode.SelectSingleNode("ns:SQL", xnsm);
            if (sqlnode == null)
            {
                throw new ApplicationException("Not Find SQL XmlNode");
            }
            action.ActionCommand = new ActionCommand();
            DbCommand cmmd = dbConfig.CreateConnection().CreateCommand();
            //action.DataAdapter = cnn.CreateDataAdapter();

            action.ActionCommand.Command = cmmd;
            cmmd.CommandText = sqlnode.InnerText;



            if (sqlnode.Attributes["Type"] != null)
            {
                //PageFields
                string type = sqlnode.Attributes["Type"].Value;
                if (type == "Text")
                {
                    cmmd.CommandType = CommandType.Text;
                }
                else if (type == "StoredProcedure")
                {
                    cmmd.CommandType = CommandType.StoredProcedure;
                }
            }

            //设置Mapping
            action.ActionCommand.ParameterMapping = InitMapping(actionNode, xnsm);

            //设置Parameter
            InitParameters(actionNode, cmmd, action, xnsm);

            //设置SearchParameter
            InitSearchParameters(actionNode, cmmd, action, xnsm);

            if (action.IsSearchAction)
            {
                //必须是Text
                if (cmmd.CommandType != CommandType.Text)
                {
                    throw new ApplicationException(string.Format("【{0}】查询命令必须为Text", action.Name));
                }
                action.IsEndWithWhereSQL = cmmd.CommandText.Trim().EndsWith("where 1=1", StringComparison.CurrentCultureIgnoreCase);
                if (action.IsEndWithWhereSQL)
                {
                    cmmd.CommandText = cmmd.CommandText.Trim().Substring(0, cmmd.CommandText.Trim().Length - 9);
                }
            }
            else
            {

                if (action.StateOperation != null)
                {
                    //生成相应SQL
                    //系统会生成StateCommand，故不需要设置CommandText
                    //cmmd.CommandText = action.GetStateCommand(p).CommandText;
                }
                dbConfig.SetTrySQL(cmmd);
            }
            //设置Result
            action.Result = InitActionResult(actionNode, xnsm);
            //
            if (actionNode.Attributes["IsSupportBatch"] != null)
            {
                action.IsSupportBatch = actionNode.Attributes["IsSupportBatch"].Value.Equals("true", StringComparison.CurrentCultureIgnoreCase)
                    || actionNode.Attributes["IsSupportBatch"].Value.Equals("1", StringComparison.CurrentCultureIgnoreCase);
            }

            if (actionNode.Attributes["IsWhereFormat"] != null)
            {
                action.IsWhereFormat = actionNode.Attributes["IsWhereFormat"].Value.Equals("true", StringComparison.CurrentCultureIgnoreCase)
                    || actionNode.Attributes["IsWhereFormat"].Value.Equals("1", StringComparison.CurrentCultureIgnoreCase);
            }

            if (actionNode.Attributes["NeedTransaction"] != null)
            {
                action.NeedTransaction = actionNode.Attributes["NeedTransaction"].Value.Equals("true", StringComparison.CurrentCultureIgnoreCase)
                    || actionNode.Attributes["NeedTransaction"].Value.Equals("1", StringComparison.CurrentCultureIgnoreCase);
            }
            else
            {
                //分析命令是否为查询SQL还是更改SQL，假如是查询SQL，则不需要事务
                if (!string.IsNullOrEmpty(cmmd.CommandText))
                {
                    if (cmmd.CommandType == CommandType.Text)
                    {
                        if (cmmd.CommandText.IndexOf("BEGIN TRAN") > 0)
                        {
                            action.NeedTransaction = false;
                        }
                        else
                        {
                            //存在UPDATE INSERT 字样需要事务
                            action.NeedTransaction = (cmmd.CommandText.IndexOf("UPDATE ", StringComparison.CurrentCultureIgnoreCase) > 0
                                || cmmd.CommandText.IndexOf("INSERT ", StringComparison.CurrentCultureIgnoreCase) > 0);
                        }
                    }
                    else
                    {
                        //存储过程需要加事务
                        action.NeedTransaction = (cmmd.CommandType == CommandType.StoredProcedure);
                    }
                }
            }
            //判断是否有特殊处理
            if (MainConfig.GlobalHanlers.ContainsKey(action.Name))
            {
                action.GlobalHandler = MainConfig.GlobalHanlers[action.Name];
            }
            if (action.Page !=null && MainConfig.PageHandlers.ContainsKey(action.Page.ID))
            {
                Dictionary<string, ActionHandlerBase> keyValues = MainConfig.PageHandlers[action.Page.ID];
                if (keyValues.ContainsKey(action.Name))
                {
                    action.Handler = keyValues[action.Name];
                }
            }
            return action;
        }
        /// <summary>
        /// 根据节点，实例化DbAction
        /// </summary>
        /// <param name="actionNode"></param>
        /// <returns></returns>
        internal static DbAction InitDbAction(XmlNode actionNode, XmlNamespaceManager xnsm,Page p)
        {
            DbAction action = InitDbAction(actionNode, xnsm, p.DbConfig);
            if(string.IsNullOrEmpty(action.ParameterDefaultTable))
            {
                action.ParameterDefaultTable = p.Name;
            }
            action.Page = p;
            
            return action;
        }
        /// <summary>
        /// 
        /// </summary>
        /// <param name="actionNode"></param>
        /// <param name="xnsm"></param>
        /// <returns></returns>
        internal static ActionResultType InitActionResult(XmlNode actionNode, XmlNamespaceManager xnsm)
        {
            ActionResultType result = null;
            XmlNode resultnode = actionNode.SelectSingleNode("ns:Result", xnsm);
            if (resultnode != null)
            {
                result = new ActionResultType();
                XmlNode simplenode = resultnode.SelectSingleNode("ns:Simple", xnsm);
                if (simplenode != null)
                {
                    result.IsSimple = true;
                    result.Simple = (ActionSimpleType)Enum.Parse(typeof(ActionSimpleType), simplenode.InnerText);
                }
                else
                {

                }
                XmlAttribute xmlAttribute = resultnode.Attributes["Description"];
                if (xmlAttribute != null)
                {
                    result.Description = xmlAttribute.Value;
                }
            }
            return result;

        }
        /// <summary>
        /// 初始化Mapping
        /// </summary>
        /// <param name="actionNode"></param>
        /// <param name="xnsm"></param>
        /// <returns></returns>
        internal static Mapping InitMapping(XmlNode actionNode, XmlNamespaceManager xnsm)
        {
            XmlNode mappingnode = actionNode.SelectSingleNode("ns:Mapping", xnsm);
            if (mappingnode == null) return null;
            Mapping mapping = new Mapping();
            XmlNodeList stringnodes = mappingnode.SelectNodes("ns:RequestKey", xnsm);
            if (stringnodes != null)
            {
                mapping.Keys = new System.Collections.Specialized.NameValueCollection();
                foreach (XmlNode n in stringnodes)
                {
                    string key = n.Attributes["KeyName"].InnerText;
                    string pname = n.Attributes["ParametrName"].InnerText;
                    mapping.Keys.Add(pname, key);
                }
            }


            return mapping;
        }
        //internal static void SetParamertType(IDbDataParameter p, DbAction action)
        //{

        //    SetParamertType(p, action.DefaultTableSchema );
        //}
        
        internal static void InitParameters(XmlNode actionNode, DbCommand cmmd
            , DbAction action, XmlNamespaceManager xnsm)
        {
            if (action.ActionCommand == null)
                action.ActionCommand = new ActionCommand();
            action.IsSearchAction = !InitParameters(actionNode, xnsm, action.ActionCommand
                , action.DefaultTableSchema,action.DbConfig);
        }
        /// <summary>
        /// 初始命令参数
        /// </summary>
        /// <param name="actionNode"></param>
        /// <param name="cmmd"></param>
        /// <param name="action"></param>
        /// <param name="xnsm"></param>
        internal static bool InitParameters(XmlNode actionNode, XmlNamespaceManager xnsm
            , ActionCommand actionCommand
            , DataTable tableSchema
            ,DbConfig dbConfig)
        {
            DbCommand cmmd = actionCommand.Command;
            Mapping parameterMapping = actionCommand.ParameterMapping;
            //DataTable tableSchema = action.DefaultTableSchema;

            XmlNodeList psnodes = actionNode.SelectNodes("ns:Parameter", xnsm);
            if (psnodes == null || psnodes.Count == 0)
            {
                return false;
            }
            cmmd.Parameters.Clear();

            foreach (XmlNode pnode in psnodes)
            {
                IDbDataParameter p = InitParameter(pnode, actionCommand, tableSchema, dbConfig);
                cmmd.Parameters.Add(p);
            }
            return true;
        }
        /// <summary>
        /// 初始命令参数
        /// </summary>
        /// <param name="actionNode"></param>
        /// <param name="cmmd"></param>
        /// <param name="action"></param>
        /// <param name="xnsm"></param>
        internal static void InitSearchParameters(XmlNode actionNode, DbCommand cmmd
            , DbAction action, XmlNamespaceManager xnsm)
        {


            XmlNodeList psnodes = actionNode.SelectNodes("ns:SearchParameter", xnsm);
            if (psnodes == null || psnodes.Count == 0)
            {
                return;
            }
            cmmd.Parameters.Clear();
            action.IsSearchAction = true;
            foreach (XmlNode pnode in psnodes)
            {
                DbParameter p = cmmd.CreateParameter();
                p.ParameterName = pnode.Attributes["Name"].Value;

                p.SourceColumn = p.ParameterName.TrimStart('@');
                if (pnode.Attributes["ShortName"] != null && pnode.Attributes["ShortName"].Value != null)
                {
                    string shortname = pnode.Attributes["ShortName"].Value.Trim();
                    if (string.IsNullOrEmpty(shortname) == false)
                    {
                        if (action.SearchParameterShortNames == null)
                        {
                            action.SearchParameterShortNames = new Dictionary<string, string>();
                        }
                        action.SearchParameterShortNames.Add(p.ParameterName, string.Format("{0}.{1}", shortname, p.SourceColumn));
                    }
                }
                if (action.ActionCommand.ParameterMapping != null && action.ActionCommand.ParameterMapping.Keys[p.SourceColumn] != null)
                {
                    p.SourceColumn = action.ActionCommand.ParameterMapping.Keys[p.SourceColumn];
                }

                if (pnode.Attributes["Type"] == null)
                {

                }
                else
                {
                    p.DbType = (System.Data.DbType)Enum.Parse(typeof(System.Data.DbType), pnode.Attributes["Type"].Value);
                    if (pnode.Attributes["Length"] != null)
                    {
                        int tempsize = int.Parse(pnode.Attributes["Length"].Value);
                        if (tempsize < 0)
                            p.Size = -1;
                        else
                            p.Size = tempsize;
                    }
                }
                if (pnode.Attributes["CanEmpty"] != null)
                {
                    p.SourceColumnNullMapping = pnode.Attributes["CanEmpty"].Value.Equals("true") ? true : false;
                }
                else
                {
                    p.SourceColumnNullMapping = true;
                }
                //判断是否为多行
                if (pnode.Attributes["Muilt"] != null)
                {
                    if(action.SearchParameterMuilts == null)
                    {
                        action.SearchParameterMuilts = new StringCollection();
                    }
                    bool flag= pnode.Attributes["Muilt"].Value.Equals("true") ? true : false;
                    if (flag)
                    {
                        action.SearchParameterMuilts.Add(p.ParameterName);
                    }
                }

                
                if (pnode.Attributes["WhereSQL"] != null)
                {
                    p.Value = pnode.Attributes["WhereSQL"].Value;
                }
                cmmd.Parameters.Add(p);
            }
        }
        /// <summary>
        /// 根据节点，生成相应的参数
        /// </summary>
        /// <param name="pnode"></param>
        /// <param name="cmmd"></param>
        /// <param name="parameterMapping"></param>
        /// <param name="tableSchema"></param>
        /// <returns></returns>
        internal static IDbDataParameter InitParameter(XmlNode pnode, ActionCommand actionCommand
            , DataTable tableSchema
            , DbConfig dbConfig)
        {
            DbCommand cmmd = actionCommand.Command;
            Mapping parameterMapping = actionCommand.ParameterMapping;

            DbParameter p = cmmd.CreateParameter();
            p.ParameterName = pnode.Attributes["Name"].Value;
            if (p.ParameterName.StartsWith("@") == false)
            {
                p.SourceColumn = p.ParameterName;
                p.ParameterName = string.Format("@{0}", p.ParameterName);
            }
            else
            {
                p.SourceColumn = p.ParameterName.TrimStart('@');
            }

            if (parameterMapping != null && parameterMapping.Keys[p.SourceColumn] != null)
            {
                p.SourceColumn = parameterMapping.Keys[p.SourceColumn];
            }

            if (pnode.Attributes["Default"] != null)
            {
                p.Value = pnode.Attributes["Default"].Value;
            }
            if (pnode.Attributes["Description"] != null)
            {
                actionCommand.ParameterDescrtions.Add(p.SourceColumn, pnode.Attributes["Description"].Value);
            }


            if (pnode.Attributes["Type"] == null)
            {

                //设置参数类型
                dbConfig.SetParamertType(p, tableSchema);
            }
            else
            {
                p.DbType = (System.Data.DbType)Enum.Parse(typeof(System.Data.DbType), pnode.Attributes["Type"].Value);
                if (pnode.Attributes["Length"] != null)
                {
                    int tempsize = int.Parse(pnode.Attributes["Length"].Value);
                    if (tempsize < 0)
                        p.Size = -1;
                    else
                        p.Size = tempsize;
                }
                if (p.DbType == DbType.Decimal)
                {
                    if (pnode.Attributes["Scale"] == null)
                    {
                        throw new ApplicationException("当为Decimal类型时，必须指定小数位数(Scale)。");
                    }
                    else
                    {
                        p.Scale = byte.Parse(pnode.Attributes["Scale"].Value);
                    }
                }
                if (p.DbType == DbType.Decimal)
                {
                    //判断是否指定了长度和小数位数
                    if (p.Precision == 0 || p.Scale == 0)
                    {
#if (DEBUG)
                        throw new ApplicationException("当为Decimal类型时，必须指定长度(Length)和小数位数(Scale)。");
#endif
                        if (p.Precision == 0)
                        {
                            p.Precision = 18;
                        }
                        if (p.Scale == 0)
                        {
                            p.Scale = 2;
                        }
                    }
                }

                if (pnode.Attributes["Direction"] != null)
                {
                    p.Direction = (ParameterDirection)Enum.Parse(typeof(ParameterDirection), pnode.Attributes["Direction"].Value);
                }

            }
            if (pnode.Attributes["CanEmpty"] != null)
            {
                p.SourceColumnNullMapping = pnode.Attributes["CanEmpty"].Value.Equals("true") ? true : false;
            }
            else
            {
                p.SourceColumnNullMapping = false;
            }
            return p;
        }
#endregion

        /// <summary>
        /// 获取当前Action邦定后的命令
        /// </summary>
        /// <param name="valueHandler"></param>
        /// <param name="userID"></param>
        /// <returns></returns>
        private ActionBindCommand GetBindCommand(ActionData valueHandler,int userID)
        {
            ActionBindCommand cmd = new ActionBindCommand();
            if(Page != null)
            {
                cmd.PageID = Page.ID;
            }
            cmd.ActionName = this.Name;

            //假如有CheckAction，则获取
            if (this.Page != null && !string.IsNullOrEmpty( this.CheckAction) )
            {
                DbAction checkaction = this.Page.GetAction(this.CheckAction);
                if (!this.IsInsert)
                {
                    BindResult CheckBindResult = MappingBind.BindAction(checkaction, valueHandler, userID, string.Empty);
                    if (CheckBindResult.IsOK)
                    {
                        cmd.CheckActionCommand = CheckBindResult.Command;
                        cmd.CheckActionStateCommand = CheckBindResult.StateCommand;
                        
                    }
                    else
                    {
                        cmd.ErrMessage = CheckBindResult.GetAllErrMessage();
                        return cmd;
                    }
                }
            }
            
            BindResult MainBindResult = MappingBind.BindAction(this, valueHandler, userID, string.Empty);
            if (MainBindResult.IsOK)
            {
                cmd.MainActioncmmd = MainBindResult.Command;
                cmd.StateCommand=MainBindResult.StateCommand;
            }
            else
            {
                cmd.ErrMessage = MainBindResult.GetAllErrMessage();
            }
            //检查是否有权限执行此命令
            if (this.FunctionID > 0)
            {
                if (ActionManager.HandlerCheckPopedom != null)
                {
                    if (!ActionManager.HandlerCheckPopedom(this, valueHandler))
                    {
                        cmd.ErrMessage = string.Format("您无权执行此操作.the page id {0} ,Function id {1}", this.Page.ID, this.FunctionID);
                        return cmd;
                    }
                }
            }

            //新增或更新命令，添加唯一键检查
            if (this.IsInsert && this.Page.InsertCheckUniqueSQL != null)
            {
                cmd.MainActioncmmd.CommandText = string.Format("{0}{1}"
                    , this.Page.InsertCheckUniqueSQL, cmd.MainActioncmmd.CommandText);
            }

            if (this.IsUpdate && this.Page.UpdateCheckUniqueSQL != null)
            {
                cmd.MainActioncmmd.CommandText = string.Format("{0}{1}"
                    , this.Page.UpdateCheckUniqueSQL, cmd.MainActioncmmd.CommandText);
            }
            cmd.ResultType = this.Result.Simple;
            return cmd;
        }
        /// <summary>
        /// 获取Action相关的所有命令信息
        /// </summary>
        /// <param name="handlerValue"></param>
        /// <param name="userID"></param>
        /// <param name="handlerCommand"></param>
        /// <param name="connectionString"></param>
        /// <returns></returns>
        private ActionBindCommand GetAllActionBindCommand(ActionData handlerValue, int userID
            , string connectionString)
        {
            ActionData mainhandlervalue = handlerValue;
            Hashtable maindata = handlerValue.GetHashTableValue("Data");
            if (maindata != null)
            {
                mainhandlervalue = new ActionData(maindata);
                mainhandlervalue.Parent = handlerValue;
            }

            ActionBindCommand bindCommand = GetBindCommand(mainhandlervalue, userID);
            if (bindCommand.IsErr)
            {
                return bindCommand;
            }

            if (this.Page == null)
                return bindCommand;

            #region//收集传出参数
            ArrayList OutFields = handlerValue.GetArrayListValue("OutFields");
            if(OutFields != null)
            {
                bindCommand.OutFields = new StringCollection();
                foreach (object OutField in OutFields)
                {
                    if (OutField != null && !string.IsNullOrEmpty    ( OutField.ToString()))
                    {
                        bindCommand.OutFields.Add(OutField.ToString());
                    }
                }
            }
            #endregion

            ArrayList actions = handlerValue.GetArrayListValue("Actions");
            if (actions != null)
            {
                ActionData actionDataParent = new ActionData(handlerValue.GetHashTableValue("Data"));
                bindCommand.ActionBindCommands = new Collection<ActionBindCommand>();
                bindCommand.BatchActionBindCommands = new Collection<BatchActionBindCommand>();
                //说明多个Action
                for (int i = 0; i < actions.Count; i++)
                {
                    Hashtable hashtable = (Hashtable)actions[i];
                    string actionname = hashtable["ActionName"].ToString();
                    if (this.Page.BatchDbActions.ContainsKey(actionname))
                    {
                        BatchDbAction batchDbAction = this.Page.BatchDbActions[actionname];
                        ActionData handlerValue1 = new ActionData(hashtable, actionDataParent);
                        
                        BatchActionBindCommand batchActionBindCommand =
                            batchDbAction.GetBatchActionBindCommand(handlerValue1, userID);

                        bindCommand.BatchActionBindCommands.Add(batchActionBindCommand);
                    }
                    else
                    {
                        ActionData handlerValue1 = new ActionData(handlerValue.GetHashTableValue("Data")
                            , handlerValue);
                        DbAction dbAction = this.Page.GetAction(actionname, handlerValue1);
                        ActionBindCommand actionBindCommand = dbAction.GetBindCommand(handlerValue1, userID);
                        bindCommand.ActionBindCommands.Add(actionBindCommand);
                    }

                    
                }
            }
            return bindCommand;
        }
        private ActionData Data;
        /// <summary>
        /// 执行Action
        /// </summary>
        /// <param name="handlerValue"></param>
        /// <param name="userID"></param>
        /// <param name="connectionString"></param>
        /// <returns></returns>
        public ActionResult Do(ActionData handlerValue,int userID
            ,string connectionString)
        {
            Data=handlerValue;
            ActionResult result = new ActionResult();
            ActionBindCommand allbindcommand= GetAllActionBindCommand(handlerValue,userID
                ,connectionString);
            if (allbindcommand.IsErr)
            {
                result.ErrMessage=allbindcommand.ErrMessage;
                return result;
            }
            allbindcommand.IsInsert = this.IsInsert;

            result = DoAction(allbindcommand, this.Result.Simple);
            System.Diagnostics.Debug.WriteLine(string.Format("DbAction【{0}】执行完成.IsErr【{1}】", this.Name, result.IsErr));
            System.Diagnostics.Debug.WriteLineIf(result.IsErr, string.Format("执行失败:{0}", result.ErrMessage));

            return result;
        }
        private ActionResult HandlerBefor(ActionBindCommand mainBindCommand
            ,ActionHandlerBase handler)
        {
            if (handler.IsHaveBefore)
            {
                ActionResult result = handler.Before();
                if (result.IsErr) return result;
            }
            if (handler.IsHaveAddParameter)
            {
                //给命令添加参数
                mainBindCommand.AddParamter = handler.AddParameter;
                mainBindCommand.AddOutParamter();
            }
            
            return new ActionResult();
        }
        /// <summary>
        /// 执行Action最底层方法
        /// </summary>
        /// <param name="action">Action命令</param>
        /// <param name="cmmd">主命令</param>
        /// <param name="statecmmd">状态命令</param>
        /// <param name="checkcmmd">检查命令</param>
        /// <param name="checkStatecmmd">检查命令对应的状态命令</param>
        /// <param name="type">执行结果返回类型</param>
        /// <returns></returns>
        ActionResult DoAction(ActionBindCommand mainBindCommand, ActionSimpleType type)
        {
            IDbCommand cmmd = mainBindCommand.MainActioncmmd;
            ActionResult result = new ActionResult();
            if (cmmd.Connection == null 
                || string.IsNullOrEmpty(cmmd.Connection.ConnectionString))
            {
                cmmd.Connection = this.DbConfig.CreateConnection();
            }
            
            int userid = int.Parse(((IDbDataParameter)cmmd.Parameters["@UserID"]).Value.ToString());
            
            if (GlobalHandler != null)
            {
                GlobalHandler.ActionData = this.Data;
                result = HandlerBefor(mainBindCommand, GlobalHandler);
                
                if (result.IsErr) return result;
            }
            if (Handler != null)
            {
                Handler.ActionData = this.Data;
                result = HandlerBefor(mainBindCommand, Handler);
                if (result.IsErr) return result;
            }
            
            using (IDbConnection cnn = cmmd.Connection)
            {
                DateTime commandbegin = DateTime.Now;
                System.Diagnostics.Debug.WriteLine(string.Format("--开始执行Action:{0}--------------------------------------------------", this.Name));
                try
                {
                    cnn.Open();
                    using (IDbTransaction tran = GetRransaction(this, cnn))
                    {

                        try
                        {
                            DbConfig dbConfig = DbConfig.GetDbConfig(DbConfig.DbType,cnn.ConnectionString);
                            dbConfig._Connection =(DbConnection)cnn;


                            Hashtable hashtable = new Hashtable();
                            result.Data = hashtable;

                            ActionResult tempresult = new ActionResult();
                            #region//外部调用处理
                            if(GlobalHandler != null && GlobalHandler.IsHaveBeforeDoCommand)
                            {
                                tempresult = GlobalHandler.Before(dbConfig);
                                if (tempresult.IsErr)
                                {
                                    RollbackTran(tran);
                                    return tempresult;
                                }
                                hashtable.Add("GBefore", tempresult.Data);
                            }
                            if (Handler != null && Handler.IsHaveBeforeDoCommand)
                            {
                                tempresult = Handler.Before(dbConfig);
                                if (tempresult.IsErr)
                                {
                                    RollbackTran(tran);
                                    return tempresult;
                                }
                                hashtable.Add("Before", tempresult.Data);
                            }

                            #endregion
                            tempresult = DoActionCommand(mainBindCommand, cnn, tran);
                            if(tempresult.IsErr) return tempresult;
                            object maindata=tempresult.Data;

                            mainBindCommand.BindOutKeyValues(maindata);
                            hashtable.Add(mainBindCommand.ActionName, tempresult.Data);
                            Collection<ActionResult> results = new Collection<ActionResult>();
                            
                            ActionBindCommand tempbindcommand;
                            if (mainBindCommand.ActionBindCommands != null)
                            {
                                for(int i = 0;i < mainBindCommand.ActionBindCommands.Count; i++)
                                {
                                    tempbindcommand = mainBindCommand.ActionBindCommands[i];
                                    tempresult = DoActionCommand(tempbindcommand, cnn, tran);
                                    if (tempresult.IsErr) return result;
                                    hashtable.Add(tempbindcommand.ActionName, tempresult.Data);
                                }
                            }
                            if (mainBindCommand.BatchActionBindCommands != null)
                            {
                                for(int i = 0; i < mainBindCommand.BatchActionBindCommands.Count; i++)
                                {
                                    BatchActionBindCommand batchActionBindCommand = mainBindCommand.BatchActionBindCommands[i];
                                    tempresult= DoBatchCommand(batchActionBindCommand, cnn, tran);
                                    if (tempresult.IsErr) return tempresult;
                                    hashtable.Add(batchActionBindCommand.ActionName, tempresult.Data);
                                }
                            }

                            #region//外部调用处理
                            
                            
                            if (GlobalHandler != null && GlobalHandler.IsHaveAfterDoCommand)
                            {
                                tempresult = GlobalHandler.After(dbConfig, result);
                                if (tempresult.IsErr)
                                {
                                    RollbackTran(tran);
                                    return tempresult;
                                }
                                hashtable.Add("GAfter", tempresult.Data);
                            }
                            if (Handler != null && Handler.IsHaveAfterDoCommand)
                            {
                                tempresult = Handler.After(dbConfig, result);
                                if (tempresult.IsErr)
                                {
                                    RollbackTran(tran);
                                    return tempresult;
                                }
                                hashtable.Add("After", tempresult.Data);
                            }
                            #endregion

                            if (tran != null && tran.Connection != null)
                            {
                                tran.Commit();
                            }
                            if (hashtable.Count == 1)
                            {
                                foreach(string key in hashtable.Keys)
                                {
                                    result.Data =hashtable[key];
                                    break;
                                }
                            }
                            #region//外部最后调用处理
                            
                            if (GlobalHandler != null && GlobalHandler.IsHaveAfter)
                            {
                                result = GlobalHandler.After(result);
                                if (result.IsErr)
                                {
                                    return result;
                                }
                            }
                            if (Handler != null && Handler.IsHaveAfter)
                            {
                                result = Handler.After(result);
                                if (result.IsErr)
                                {
                                    return result;
                                }
                            }
                            #endregion
                        }
                        catch (DbException dberr)
                        {
                            if (tran != null && tran.Connection != null)
                            {
                                tran.Rollback();
                            }
                            System.Diagnostics.Debug.WriteLine(string.Format("DoAction Err:{0}", dberr.ToString()));
                            result.SetError(dberr);
                        }
                        catch (Exception err)
                        {
                            if (tran != null && tran.Connection != null)
                            {
                                tran.Rollback();
                            }
                            result.SetError(err);
                        }
                    }
                }
                catch (DbException opendberr)
                {
                    result.SetError(opendberr);
                }
                catch (Exception openerr)
                {
                    result.SetError(openerr);
                }
            }
            return result;
        }
        ActionResult DoActionCommand(ActionBindCommand bindCommand
            ,IDbConnection cnn, IDbTransaction tran)
        {
            IDbCommand cmmd = bindCommand.MainActioncmmd;
            IDbCommand statecmmd = bindCommand.StateCommand;
            IDbCommand checkcmmd = bindCommand.CheckActionCommand;
            IDbCommand checkStatecmmd = bindCommand.CheckActionStateCommand;
            ActionResult result = new ActionResult();
            IDbCommand currentcmmd = cmmd;
            try
            {
                ActionResult cmmdresult = new ActionResult();
                object executeScalar;
                string err = string.Empty;
                if (checkStatecmmd != null)
                {
                    cmmdresult = DoDbCommand(checkStatecmmd, cnn, tran
                        , ActionSimpleType.OneValue,"CheckStateCommand");
                    if (cmmdresult.IsOK)
                    {
                        executeScalar = cmmdresult.Data.ToString();
                    }
                    else
                    {
                        return cmmdresult;
                    }

                    if (executeScalar != null)
                    {
                        result.ErrMessage = executeScalar.ToString();
                    }
                    if (result.IsErr)
                    {
                        RollbackTran(tran);
                        return result;
                    }
                }
                if (checkcmmd != null && string.IsNullOrEmpty(checkcmmd.CommandText) == false)
                {
                    cmmdresult = DoDbCommand(checkcmmd, cnn, tran
                        , ActionSimpleType.OneValue, "CheckCommand");
                    if (cmmdresult.IsOK)
                    {
                        executeScalar = cmmdresult.Data.ToString();
                    }
                    else
                    {
                        return cmmdresult;
                    }

                    if (executeScalar != null)
                    {
                        result.ErrMessage = executeScalar.ToString();
                    }
                    if (result.IsErr)
                    {
                        RollbackTran(tran);
                        return result;
                    }
                }
                if (statecmmd != null)
                {
                    cmmdresult = DoDbCommand(statecmmd, cnn, tran
                        , ActionSimpleType.OneValue, "StateCommand");
                    if (cmmdresult.IsOK)
                    {
                        executeScalar = cmmdresult.Data.ToString();
                    }
                    else
                    {
                        return cmmdresult;
                    }

                    if (executeScalar != null)
                    {
                        result.ErrMessage = executeScalar.ToString();
                    }
                    if (result.IsErr)
                    {
                        RollbackTran(tran);
                        return result;
                    }
                }

                if (string.IsNullOrEmpty(cmmd.CommandText) == false)
                {
                    cmmdresult = DoDbCommand(cmmd, cnn, tran
                        , bindCommand.ResultType,string.Empty);
                    return cmmdresult;
                }
            }
            catch (DbException dberr)
            {
                RollbackTran(tran);
                result.SetError(dberr);
            }
            catch (Exception err)
            {
                RollbackTran(tran);
                result.SetError(err);
            }

            return result;
        }
        void RollbackTran(IDbTransaction tran)
        {
            if (tran != null && tran.Connection != null)
            {
                tran.Rollback();
            }
        }
        /// <summary>
        /// 执行命令
        /// </summary>
        /// <param name="dbCommand"></param>
        /// <param name="cnn"></param>
        /// <param name="tran"></param>
        /// <param name="simpleType"></param>
        /// <returns></returns>
        ActionResult DoDbCommand(IDbCommand dbCommand, IDbConnection cnn
            , IDbTransaction tran, ActionSimpleType simpleType,string tipinfo)
        {
            ActionResult result = new ActionResult();
            DbDataAdapter adapter = null;
            DateTime commandbegin = DateTime.Now;
            dbCommand.Connection = cnn;
            dbCommand.Transaction = tran;
            try
            {
                switch (simpleType)
                {
                    case ActionSimpleType.NonQuery:
                        result.Data = dbCommand.ExecuteNonQuery();
                        break;
                    case ActionSimpleType.OneValue:
                        result.Data = dbCommand.ExecuteScalar();
                        break;
                    case ActionSimpleType.OneRow:
                        adapter = this.DbConfig.CreateDataAdapter();
                        adapter.SelectCommand = (DbCommand)dbCommand;
                        DataTable dtonerow = new DataTable(this.Name);
                        adapter.Fill(dtonerow);
                        if (dtonerow.Rows.Count > 0)
                            result.Data = dtonerow.Rows[0];
                        else
                            result.Data = null;
                        break;
                    case ActionSimpleType.DataTable:
                        adapter = this.DbConfig.CreateDataAdapter();
                        adapter.SelectCommand = (DbCommand)dbCommand;
                        DataTable dt = new DataTable(this.Name);
                        adapter.Fill(dt);
                        result.Data = dt;
                        break;
                    case ActionSimpleType.DataSet:
                        adapter = this.DbConfig.CreateDataAdapter();
                        adapter.SelectCommand = (DbCommand)dbCommand;
                        DataSet ds = new DataSet(this.Name);
                        adapter.Fill(ds);
                        result.Data = ds;
                        break;
                }

                TimeSpan timespan = (DateTime.Now - commandbegin);
                if (MainConfig.IsEncryption == false)
                {
                    
                    if (!string.IsNullOrEmpty(tipinfo)) tipinfo = string.Format("-{0}", tipinfo);
                    CommandLog.Log(string.Format("{0}{1}{2}", this.Name, tipinfo,tran==null?"":"开启事务"),dbCommand,
                        result.Data,commandbegin,this.DbConfig);
                }
                
                if (timespan.TotalSeconds > TimeOutLog)
                {
                    if (LogOutCommand != null)
                    {
                        try
                        {
                            Thread thread = new Thread(WriteLogOutCommand);
                            CommandInfo commandInfo = new CommandInfo();
                            commandInfo.ActionName = this.Name;
                            commandInfo.TotalSeconds = timespan.TotalSeconds;
                            commandInfo.DbCommand = DbConfig.GetCommandString(dbCommand);
                            thread.Start(commandInfo);
                        }
                        catch (Exception err)
                        {
                            ActionManager.ErrLog.AddLogInfo("记录超时命令", err.ToString());
                        }

                    }
                }
            }
            catch (DbException dberr)
            {
                RollbackTran(tran);
                result.SetError(dberr);
                if(!string.IsNullOrEmpty(tipinfo)) tipinfo=string.Format("-{0}",tipinfo);
                Log.Write(string.Format("DoAction:{0}{1}", this.Name, tipinfo)
                    , dbCommand, dberr, this.DbConfig);
            }catch(Exception err)
            {
                RollbackTran(tran);
                result.SetError(err);
                if (!string.IsNullOrEmpty(tipinfo)) tipinfo = string.Format("-{0}", tipinfo);
                Log.Write(string.Format("DoAction:{0}{1}", this.Name, tipinfo)
                    , dbCommand, err, this.DbConfig);
            }
            
            return result;
        }
        void WriteLogOutCommand(object commandInfo)
        {
            if (LogOutCommand != null)
                LogOutCommand((CommandInfo)commandInfo);
        }

        ActionResult DoBatchCommand(BatchActionBindCommand batchActionBindCommand
            , IDbConnection cnn, IDbTransaction tran)
        {
            IDbCommand checkactioncmmd = batchActionBindCommand.CheckActionCommand;
            IDbCommand checkactionstatecmmd = batchActionBindCommand.CheckActionStateCommand;
            IDbCommand checkcmmd = batchActionBindCommand.CheckCommand;
            
            ActionResult result = new ActionResult();

            try
            {
                string err = string.Empty;
                //object executeScalar;
                string actionname = batchActionBindCommand.ActionName;
                if (checkactionstatecmmd != null)
                {
                    result = DoDbCommand(checkactionstatecmmd, cnn, tran
                        , ActionSimpleType.OneValue, actionname+"-CheckActionStateCommand");
                    if (result.IsOK)
                    {
                        result.ErrMessage = result.Data.ToString();
                    }
                    if (result.IsErr)
                    {
                        RollbackTran(tran);
                        return result;
                    }
                }
                if(checkactioncmmd != null)
                {
                    result = DoDbCommand(checkactioncmmd, cnn, tran
                        , ActionSimpleType.OneValue, actionname + "-CheckActionCommand");
                    if (result.IsOK)
                    {
                        result.ErrMessage = result.Data.ToString();
                    }
                    if (result.IsErr)
                    {
                        RollbackTran(tran);
                        return result;
                    }
                }
                if (checkcmmd != null && string.IsNullOrEmpty(checkcmmd.CommandText) == false)
                {
                    result = DoDbCommand(checkactioncmmd, cnn, tran
                        , ActionSimpleType.OneValue, actionname + "-CheckCommand");
                    if (result.IsOK)
                    {
                        result.ErrMessage = result.Data.ToString();
                    }
                    if (result.IsErr)
                    {
                        RollbackTran(tran);
                        return result;
                    }
                }
                foreach(DbCommand command in batchActionBindCommand.DoCommands)
                {
                    result = DoDbCommand(command, cnn, tran
                        , ActionSimpleType.NonQuery, actionname + "-DoCommand");
                    if (result.IsErr)
                    {
                        RollbackTran(tran);
                        return result;
                    }
                }
                result = DoDbCommand(batchActionBindCommand.ResultCommand
                    , cnn, tran,  ActionSimpleType.DataTable, actionname + "-ResultCommand");



            }
            catch (DbException dberr)
            {
                RollbackTran(tran);
                result.SetError(dberr);
            }
            catch (Exception err)
            {
                RollbackTran(tran);
                result.SetError(err);

            }

            return result;
        }
        /// <summary>
        /// 获取事务
        /// </summary>
        /// <param name="action"></param>
        /// <param name="cnn"></param>
        /// <returns></returns>
        private IDbTransaction GetRransaction(DbAction action, IDbConnection cnn)
        {
            if (action.NeedTransaction)
                return cnn.BeginTransaction(IsolationLevel.RepeatableRead);
            else
                return null;
        }
        /// <summary>
        /// 获取SQL中的参数
        /// </summary>
        /// <param name="sql"></param>
        /// <returns></returns>
        public static StringCollection GetParameters(string sql)
        {
            StringCollection values = new StringCollection();
            Regex reg = new Regex(@"(@{1,2})\w+?\b");
            MatchCollection m = reg.Matches(sql);
            foreach (Match mm in m)
            {
                if (mm.Value.StartsWith("@@") == false && Regex.Match(sql, "declare( {1,})" + mm.Value, RegexOptions.IgnoreCase).Success == false)
                {
                    if (mm.Value.Equals("@USERID", StringComparison.CurrentCultureIgnoreCase))
                    {
                        continue;
                    }
                    if (!values.Contains(mm.Value))
                    {
                        values.Add(mm.Value);
                        System.Diagnostics.Debug.WriteLine(mm.Value);
                    }

                }

            }
            return values;
        }
        /// <summary>
        /// 获取SQL中参数XML节点
        /// </summary>
        /// <param name="sql"></param>
        /// <returns></returns>
        public static string GetParameterXMLNode(string sql)
        {
            StringCollection ps = GetParameters(sql);
            StringBuilder sb = new StringBuilder();
            foreach (string p in ps)
            {
                sb.AppendFormat("<Parameter Name=\"{0}\" Description=\"\"/>", p);
                sb.AppendLine();
            }
            System.Diagnostics.Debug.WriteLine(sb.ToString());
            return sb.ToString();
        }

        
        public string GetXMLParameterElements()
        {
            return BatchDbAction.GetXMLParameterElements(DbConfig,this.ParameterDefaultTable, this.ActionCommand.Command.CommandText);
        }
        /// <summary>
        /// 获取DbAction对应的SQL命令
        /// </summary>
        /// <returns></returns>
        public string GetTestSQLCommand()
        {
           return DbConfig.GetTestSQLCommand(this);
        }

        DbCommand _StateCommand;
        /// <summary>
        /// 状态命令
        /// </summary>
        public DbCommand GetStateCommand()
        {

            if (_StateCommand == null)
            {
                _StateCommand = GetStateCommandByPageConfig(this.Page);
            }
            return _StateCommand;
        }

        private DbCommand GetStateCommandByPageConfig(Page page)
        {
            if (StateOperation == null) return null;

            DbCommand cmmd = page.DbConfig.GetStateOperationCommand(StateOperation, page.Name, page.PKField);
            //设置Parameter
            try
            {
                DbParameter p = cmmd.Parameters[0];
                DbConfig dbConfig = MainConfig.DbConfigs[page.DbFlag];
                DataTable schema = GetTableSchema(dbConfig, page.Name);
                dbConfig.SetParamertType(p, schema);
                return cmmd;
            }
            catch (Exception err)
            {
                throw new ApplicationException("GetStateCommandByPageConfig未能获取TableSchemaCache", err);
            }
        }
    }

    public class Mapping
    {
        /// <summary>
        /// String Key
        /// </summary>
        public NameValueCollection Keys { get; set; }
        
    }

   
}
