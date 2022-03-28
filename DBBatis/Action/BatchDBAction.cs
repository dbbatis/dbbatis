using System;
using System.Text;
using System.Data;
using System.Data.Common;
using System.Xml;
using System.Text.RegularExpressions;
using System.Collections.Specialized;

namespace DBBatis.Action
{
    /// <summary>
    /// 批量Action
    /// </summary>
    public class BatchDbAction
    {
        /// <summary>
        /// 所属Page
        /// </summary>
        public Page Page{ get; internal set; }
        /// <summary>
        /// 描述
        /// </summary>
        public string Description { get; set; }
        /// <summary>
        /// 名称
        /// </summary>
        public string Name { get; set; }
        /// <summary>
        /// 对应的FunctionID,假如指定，系统获取验证权限，前提需要BILLID
        /// </summary>
        public UInt16 FunctionID { get; set; }
        /// <summary>
        /// 参数默认对应的表
        /// </summary>
        public string ParameterDefaultTable { get; set; }
        /// <summary>
        /// 检查命令
        /// </summary>
        public string CheckAction { get; set; }
        /// <summary>
        /// 检查命令
        /// </summary>
        public ActionCommand CheckCommand { get; set; }
        /// <summary>
        /// 执行命令
        /// </summary>
        public ActionCommand Command { get; set; }
        /// <summary>
        /// 返回命令
        /// </summary>
        public ActionCommand ResultCommand { get; set; }
        /// <summary>
        /// 链接信息
        /// </summary>
        public DbConfig DbConfig { get; set; }
        /// <summary>
        /// 命令
        /// </summary>
        public DbDataAdapter DataAdapter { get; set; }

        /// <summary>
        /// 参数映射
        /// </summary>
        public Mapping ParameterMapping { get; set; }
        

        public DataTable DefaultTableSchema { get; internal set; }

        internal BatchActionBindCommand GetBatchActionBindCommand(ActionData valueHandler,int userID)
        {
            StringBuilder sb = new StringBuilder();
            BatchActionBindCommand dbactioncmd = new BatchActionBindCommand();
            dbactioncmd.ActionName = this.Name;
            //假如有CheckAction，则获取
            if (this.Page != null && !string.IsNullOrEmpty( this.CheckAction))
            {
               DbAction checkaction = this.Page.GetAction(this.CheckAction);
               BindResult checkbindresult = MappingBind.BindAction(checkaction, valueHandler, userID, string.Empty);
                if (checkbindresult.IsOK)
                {
                    dbactioncmd.CheckActionCommand= checkbindresult.Command;
                    dbactioncmd.CheckActionStateCommand = checkbindresult.StateCommand;
                }
                else
                {
                    dbactioncmd.ErrMessage = String.Format("{0}:{1}", CheckAction, checkbindresult.GetEmptyKeys());
                    sb.Append(checkbindresult.GetEmptyKeys());
                }
            }
            if (dbactioncmd.IsErr) return dbactioncmd;

            BindBatchResult batchresult = MappingBind.BindBatchAction(this, valueHandler,userID);
            if (batchresult.IsErr)
            {
                dbactioncmd.ErrMessage = batchresult.GetAllErrMessage();
            }
            else
            {
                dbactioncmd.CheckCommand = batchresult.CheckCommandBind.Command;
                
                int count = batchresult.DoCommandBinds.Count;
                dbactioncmd.DoCommands = new DbCommand[count];
                for (int i = 0; i < count; i++)
                {
                    DbCommand cmmd = batchresult.DoCommandBinds[i].Command;
                    dbactioncmd.DoCommands[i] = cmmd;
                }
                dbactioncmd.ResultCommand = batchresult.ResultCommandBind.Command;
            }
            return dbactioncmd;
        }

        public string GetTestSQLCommand()
        {
            Page p = this.Page;
            StringBuilder sb = new StringBuilder();
            if (string.IsNullOrEmpty(this.CheckAction) == false)
            {
                if (p.DbActions.ContainsKey(this.CheckAction))
                {
                    sb.Append(p.DbActions[this.CheckAction].GetTestSQLCommand());
                }
                else
                {
                    sb.Append(string.Format("未检查到CheckAction【{0}】", this.CheckAction));
                }
            }
            string tempsql = string.Empty;
            if(CheckCommand != null)
            {
                tempsql = this.DbConfig.GetTestSQLCommand(CheckCommand);// DbAction.GetTestBatchSQLCommand(CheckCommand);
                sb.Append(tempsql);
                sb.AppendLine();
                sb.AppendFormat("{0}检查命令结束{0}", "".PadLeft(20, '-'));
                sb.AppendLine();
            }
            sb.AppendFormat("{0}执行命令开始{0}", "".PadLeft(20, '-'));
            sb.AppendLine();
            tempsql = this.DbConfig.GetTestSQLCommand(Command);// DbAction.GetTestBatchSQLCommand(Command);
            sb.Append(tempsql);
            sb.AppendLine();
            sb.AppendFormat("{0}执行命令结束{0}", "".PadLeft(20, '-'));
            sb.AppendLine();

            sb.AppendFormat("{0}返回命令开始{0}", "".PadLeft(20, '-'));
            sb.AppendLine();
            sb.AppendFormat("{0}--执行命令返回信息", "DECLARE @AffectedIDS VARCHAR(MAX)".PadRight(32));
            sb.AppendLine();
            sb.AppendFormat("SET     @AffectedIDS = '';");
            sb.AppendLine();
            tempsql = this.DbConfig.GetTestSQLCommand(ResultCommand); //DbAction.GetTestBatchSQLCommand(ResultCommand);
            sb.Append(tempsql);
            sb.AppendLine();
            return sb.ToString();

        }

        #region//初始Action
        /// <summary>
        /// 根据节点，实例化DbAction
        /// </summary>
        /// <param name="batchActionNode"></param>
        /// <param name="xnsm"></param>
        /// <param name="cnn"></param>
        /// <param name="page"></param>
        /// <returns></returns>
        internal static BatchDbAction InitBatchDBAction(XmlNode batchActionNode
            , XmlNamespaceManager xnsm
            ,Page page)
        {
            BatchDbAction action = new BatchDbAction();
            action.DbConfig = page.DbConfig;
            action.Page = page;
            action.Name = batchActionNode.Attributes["Name"].Value;
            
            if (batchActionNode.Attributes["ParameterDefaultTable"] != null)
            {
                action.ParameterDefaultTable = batchActionNode.Attributes["ParameterDefaultTable"].Value;
                action.DefaultTableSchema =DbAction.GetTableSchema(page.DbConfig, action.ParameterDefaultTable);
            }
            else
            {
                action.ParameterDefaultTable = null;
            }

            if (batchActionNode.Attributes["FunctionID"] != null)
            {
                action.FunctionID = UInt16.Parse(batchActionNode.Attributes["FunctionID"].Value);
            }
            else
            {
                action.FunctionID = 0;
            }
            if (batchActionNode.Attributes["CheckAction"] != null)
            {
                action.CheckAction = batchActionNode.Attributes["CheckAction"].Value;
            }
            else
            {
                action.CheckAction = string.Empty;
            }
            XmlNode descriptionNode = batchActionNode.SelectSingleNode("ns:Description", xnsm);
            if (descriptionNode != null)
            {
                action.Description = descriptionNode.InnerText;
            }

            DbCommand cmmd = (DbCommand)page.DbConfig.CreateConnection().CreateCommand();
            action.DataAdapter = page.DbConfig.CreateDataAdapter();

            XmlNode tempnode = batchActionNode.SelectSingleNode("ns:CheckCommand", xnsm);
            if(tempnode != null)
            {
                action.CheckCommand = InitActionCommand(tempnode, xnsm, page.DbConfig, action.DefaultTableSchema);
            }
            else
            {
                action.CheckCommand = null;
            }
            tempnode = batchActionNode.SelectSingleNode("ns:DoCommand", xnsm);
            if (tempnode != null)
            {
                action.Command = InitActionCommand(tempnode, xnsm, page.DbConfig, action.DefaultTableSchema);
            }
            else
            {
                throw new ApplicationException("Not Find DoCommand XmlNode");
            }
            tempnode = batchActionNode.SelectSingleNode("ns:ResultCommand", xnsm);
            if (tempnode != null)
            {
                action.ResultCommand = InitActionCommand(tempnode, xnsm, page.DbConfig, action.DefaultTableSchema);
            }
            else
            {
                throw new ApplicationException("Not Find ResultCommand XmlNode");
            }
            return action;
        }

        internal static ActionCommand InitActionCommand(XmlNode commandNode, XmlNamespaceManager xnsm
            , DbConfig cnn, DataTable tableSchema)
        {
            ActionCommand actioncommand = new ActionCommand();
            //
            XmlNode sqlnode = commandNode.SelectSingleNode("ns:SQL", xnsm);
            if (sqlnode == null)
            {
                throw new ApplicationException("Not Find SQL XmlNode");
            }
            DbCommand cmmd = (DbCommand)cnn.CreateConnection().CreateCommand();
            actioncommand.Command = cmmd;
            cmmd.CommandText = sqlnode.InnerText;
            if (sqlnode.Attributes["Type"] != null && sqlnode.Attributes["Type"].Value == "StoredProcedure")
            {
                cmmd.CommandType = CommandType.StoredProcedure;
            }

            //设置Mapping
            actioncommand.ParameterMapping =DbAction.InitMapping(commandNode, xnsm);

            //设置Parameter
            DbAction.InitParameters(commandNode, xnsm, actioncommand, tableSchema
                , cnn);

            return actioncommand;
        }


        #endregion


        #region//解析参数
        public static StringCollection ParseParameters(string sql)
        {
            StringCollection result = new StringCollection();
            //Regex paramReg = new Regex(@"@\w*");
            //2009-2-29 修正正则表达式匹配参数时，Sql中包括@@rowcount之类的变量的情况，不应该算作参数
            Regex paramReg = new Regex(@"[^@@](?<p>@\w+)");
            MatchCollection matches = paramReg.Matches(String.Concat(sql, " "));
            foreach (Match m in matches)
            {
                string p = m.Groups["p"].Value;
                if (result.IndexOf(p) == -1)
                {
                    result.Add(p);
                }
            }
            return result;
        }

        public string GetXMLParameterElements()
        {
            return GetXMLParameterElements(DbConfig,this.ParameterDefaultTable, this.Command.Command.CommandText);
        }

        public static string GetXMLParameterElements(DbConfig dbConfig,string tableName, string sql)
        {
            StringCollection ps = GetParameters(sql);
            StringBuilder sb = new StringBuilder();
            
            DataTable schema = null;
            if (string.IsNullOrEmpty(tableName) == false && ps != null && ps.Count > 0)
            {
                schema = DbAction.GetTableSchema(dbConfig, tableName);
                
            }
            foreach (string vv in ps)
            {
                if (vv.Equals("@UserID", StringComparison.CurrentCultureIgnoreCase))
                    continue;
                string desc = string.Empty;
                if(schema != null)
                {
                    DataRow[] rows = schema.Select(string.Format("name='{0}'", vv.TrimStart('@').Trim()));

                    if (rows.Length == 1)
                    {
                        desc = rows[0]["description"].ToString();

                    }
                }
                
                sb.AppendLine(string.Format("<Parameter Name=\"{0}\"  Description=\"{1}\"/>", vv, desc));
            }
            return sb.ToString();
        }

        public static StringCollection GetParameters(string sql)
        {
            //string sql = this.Command.Command.CommandText;
            StringCollection ps = ParseParameters(sql);


            StringBuilder sb = new StringBuilder();
            StringCollection declareps = new StringCollection();
            using (System.IO.TextReader reader = new System.IO.StringReader(sql))
            {
                string tempv = string.Empty;
                string v = reader.ReadLine();
                while (v != null)
                {
                    v = v.Trim();
                    if (v.Length > 8 && v.Substring(0, 7).ToUpper() == "DECLARE")
                    {
                        v = v.Substring(7).Trim();
                        string[] vs = v.Split(',');
                        foreach (string p in vs)
                        {
                            tempv = tempv.Replace('\t', ' ');
                            tempv = p.Trim().Split(' ')[0];
                            if (tempv.Length > 0 && tempv.IndexOf('@') == 0)
                            {
                                if (ps.IndexOf(tempv) != -1)
                                    ps.Remove(tempv);
                            }
                        }
                    }

                    v = reader.ReadLine();
                }
                reader.Close();
            }
            return ps;
        }
        #endregion
    }
}
