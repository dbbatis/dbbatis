using System;
using System.Collections.Specialized;
using System.Text;
using System.Data;
using System.Collections;
using System.Collections.Generic;
using DBBatis.Security;
using DBBatis.JSON;
using System.Data.Common;
using System.IO;

namespace DBBatis.Action
{
    ///// <summary>
    ///// 批量命令数据编码处理委托
    ///// </summary>
    ///// <param name="batchAction"></param>
    ///// <param name="data">解码前的数据</param>
    ///// <returns>解码后的数据</returns>
    //public delegate string BatchDataEnCodeHandler(BatchDbAction batchAction, string data);
    /// <summary>
    /// 添加Action全局参数
    /// </summary>
    /// <param name="action">Action</param>
    /// <param name="cmmd">命令</param>
    /// <param name="userID">当前用户</param>
    public delegate void AddParameterHandler(IDbCommand cmmd, int userID);
    ///// <summary>
    ///// 添加BatchAction全局参数
    ///// </summary>
    ///// <param name="batchAction">Action</param>
    ///// <param name="cmmd">命令</param>
    ///// <param name="userID">当前用户</param>
    //public delegate void AddBatchActionParameterHandler(BatchDbAction batchAction, IDbCommand cmmd, int userID);


    /// <summary>
    /// 绑定结果类
    /// </summary>
    public class BindResult
    {
        public BindResult()
        {
            EmptyKeys = new StringCollection();
        }
        public DbCommand Command { get; set; }
        public DbCommand StateCommand { get; set; }
        public System.Collections.Specialized.StringCollection EmptyKeys { get; set; }
        public string GetEmptyKeys()
        {
            if (EmptyKeys.Count == 0) return string.Empty;

            string[] emptykeys = new string[EmptyKeys.Count];
            EmptyKeys.CopyTo(emptykeys, 0);
            return string.Format("{0}不能为空.", string.Join(",", emptykeys));
        }

        /// <summary>
        /// 获取所有错误信息
        /// </summary>
        /// <returns></returns>
        public string GetAllErrMessage()
        {
            return string.Format("{0}{1}", ErrInfo, GetEmptyKeys());
        }
        internal string ErrInfo { get; set; }
        /// <summary>
        /// 判断绑定是否成功
        /// </summary>
        public bool IsOK
        {
            get
            {
                if (EmptyKeys != null && EmptyKeys.Count > 0)
                    return false;
                if (string.IsNullOrEmpty(ErrInfo) == false)
                {
                    return false;
                }
                return true;
            }
        }
        internal void AddGlobalParameter(int userid)
        {

            if (StateCommand != null)
            {
                MappingBind.AddUserParameter(StateCommand, userid);
                MappingBind.AddParameterHandler?.Invoke(StateCommand, userid);
            }
            if (Command != null)
            {
                MappingBind.AddUserParameter(Command, userid);
                MappingBind.AddParameterHandler?.Invoke(Command, userid);
            }
        }
    }

    /// <summary>
    /// 批量命令绑定
    /// </summary>
    public class BindBatchResult
    {
        public BindBatchResult()
        {
            CheckCommandBind = new BindResult();
            DoCommandBinds = new System.Collections.ObjectModel.Collection<BindResult>();
            ResultCommandBind = new BindResult();
        }
        public BindResult CheckCommandBind { get; set; }

        public System.Collections.ObjectModel.Collection<BindResult> DoCommandBinds { get; set; }

        public BindResult ResultCommandBind { get; set; }
        /// <summary>
        /// 获取不为空提示
        /// </summary>
        /// <returns></returns>
        public string GetEmptyKeys()
        {
            StringBuilder sb = new StringBuilder();

            string temp = CheckCommandBind.GetEmptyKeys();
            if (temp.Length > 0)
            {
                sb.AppendFormat("检查命令:{0}", temp);
                sb.AppendLine();
            }
            int i = 0;
            foreach (BindResult r in DoCommandBinds)
            {
                i++;
                temp = r.GetEmptyKeys();
                if (temp.Length > 0)
                {
                    sb.AppendFormat("命令[{0}]:{1}",i, temp);
                    sb.AppendLine();
                }
            }
            temp = ResultCommandBind.GetEmptyKeys();
            if (temp.Length > 0)
            {
                sb.AppendFormat("返回命令:{1}", temp);
                sb.AppendLine();
            }
            return sb.ToString();
        }
        /// <summary>
        /// 获取所有错误信息
        /// </summary>
        /// <returns></returns>
        public string GetAllErrMessage()
        {
            return string.Format("{0}{1}", ErrInfo, GetEmptyKeys());
        }
        /// <summary>
        /// 错误信息
        /// </summary>
        internal string ErrInfo { get; set; }
        /// <summary>
        /// 是否有错误
        /// </summary>
        public bool IsErr
        {
            get
            {
                bool iserr = !string.IsNullOrEmpty(ErrInfo);
                if (iserr == false)
                {
                    ErrInfo = GetEmptyKeys();
                    iserr = !string.IsNullOrEmpty(ErrInfo);
                }
                
                return iserr;
            }
        }

        internal void AddGlobalParameter(int userid)
        {

            if (CheckCommandBind != null)
            {
                CheckCommandBind.AddGlobalParameter(userid);
            }
            if (ResultCommandBind != null)
            {
                ResultCommandBind.AddGlobalParameter(userid);
            }
            if (DoCommandBinds != null)
            {
                for (int i = 0; i < DoCommandBinds.Count; i++)
                {
                    DoCommandBinds[i].AddGlobalParameter(userid);
                }
            }
        }
    }
    /// <summary>
    /// 参数绑定类
    /// </summary>
    public class MappingBind
    {
        /// <summary>
        /// 批量命令数据编码处理委托,默认UrlDecode
        /// </summary>
        //public static BatchDataEnCodeHandler BatchDataEnCodeHandler { get; set; }
        /// <summary>
        /// 添加Action全局参数
        /// </summary>
        public static AddParameterHandler AddParameterHandler { get; set; }
        


        /// <summary>
        /// 添加默认参数
        /// </summary>
        /// <param name="cmmd"></param>
        internal static void AddUserParameter(IDbCommand cmmd,int userID)
        {
            if (cmmd.Parameters.Contains("@UserID"))
            {
                throw new ApplicationException("Action中不能出现@UserID参数");
            }
            else
            {
                //自动添加当前用户参数
                IDbDataParameter userp = cmmd.CreateParameter();
                userp.ParameterName = "@UserID";
                userp.DbType = System.Data.DbType.Int32;
                userp.Value = userID;
                cmmd.Parameters.Add(userp);
            }
        }
        
        #region//绑定参数新方法
        /// <summary>
        /// 绑定命令
        /// </summary>
        /// <param name="dbaction"></param>
        /// <param name="getValueHanlder"></param>
        /// <returns></returns>
        public static BindResult BindAction(DbAction dbaction, ActionData getValueHanlder,int userID, string cnnstr)
        {
            BindResult bindResult = new BindResult();

            if (dbaction.NeedLogin)
                System.Diagnostics.Debug.Assert(userID > 0, "当前用户ID为0");
            if(dbaction.StateOperation != null)
            {
                bindResult = MappingBind.BindCommandParameters(
                       dbaction.GetStateCommand()
                       , getValueHanlder
                       , dbaction.ActionCommand
                       , dbaction.DbConfig);
                if (bindResult.IsOK)
                {
                    bindResult.StateCommand = bindResult.Command;
                }
                else
                {
                    return bindResult;
                }
            }
            

            bindResult.Command = dbaction.DbConfig.CloneCommand(dbaction.ActionCommand.Command);
            
            bool canempty = true;

            string requestValue = string.Empty;
            if (dbaction.IsSearchAction)
            {
                #region//查询参数绑定
                System.Diagnostics.Debug.WriteLine(string.Format("Begin Bind SearchAction:{0}", dbaction.Name));
                StringBuilder sbwhere = new StringBuilder();
                foreach (IDbDataParameter p in dbaction.ActionCommand.Command.Parameters)
                {
                    string wheresql = (string)p.Value;
                    
                    requestValue = getValueHanlder[p.SourceColumn];
                    canempty = ((System.Data.Common.DbParameter)p).SourceColumnNullMapping;
                    System.Diagnostics.Debug.WriteLine(string.Format("RequestKey:【{0}】VALUE:【{1}】", p.SourceColumn, requestValue));

                    if (canempty == false && string.IsNullOrEmpty(requestValue))
                    {
                        string p_description = GetDescBySourceName(dbaction.ActionCommand, p.SourceColumn);
                        bindResult.EmptyKeys.Add(p_description);
                        System.Diagnostics.Debug.WriteLine(string.Format("Error:RequestKey【{0}】不允许为空!", p.SourceColumn));
                    }
                    else
                    {
                        if (string.IsNullOrEmpty(requestValue))
                        {
                            //移除参数
                            bindResult.Command.Parameters.RemoveAt(p.ParameterName);
                            continue;
                        }
                        string[] values = null;
                        bool muiltflag = false;
                        //判断是否为多行情况
                        if (dbaction.SearchParameterMuilts != null && dbaction.SearchParameterMuilts.Contains(p.ParameterName))
                        {
                            values = requestValue.Replace("'", "''").Split(Environment.NewLine.ToCharArray());
                            StringCollection newvalues = new StringCollection();
                            foreach (string v in values)
                            {
                                string tempv = v.Trim();
                                if (string.IsNullOrEmpty(tempv))
                                    continue;
                                newvalues.Add(tempv);

                            }
                            if (newvalues.Count > 1)
                            {
                                values = new string[newvalues.Count];
                                newvalues.CopyTo(values, 0);
                                muiltflag = true;
                                //多行将不需要参数
                                bindResult.Command.Parameters.RemoveAt(p.ParameterName);
                            }
                        }//判断是否为多行情况
                         //p.Value设定的是WhereSQL，假如为NULL,则是最简单的=形式
                        if (p.Value == null)
                        {
                            string flag = " = ";
                            string searchfield = string.Empty;
                            if (dbaction.SearchParameterShortNames != null && dbaction.SearchParameterShortNames.ContainsKey(p.ParameterName))
                            {
                                searchfield = dbaction.SearchParameterShortNames[p.ParameterName];
                            }
                            else
                            {
                                searchfield = p.ParameterName.TrimStart('@');
                            }
                            if (muiltflag)
                            {

                                StringBuilder sb = new StringBuilder();
                                foreach (string v in values)
                                {
                                    sb.AppendFormat("{0}='{1}' OR ", searchfield, v);
                                }
                                if (sb.Length > 0)
                                {
                                    wheresql = string.Format("({0})", sb.ToString().TrimEnd(" OR ".ToCharArray()));
                                }
                            }
                            else
                            {
                                if (requestValue.IndexOf('%') != -1 || requestValue.IndexOf('_') != -1
                             || (requestValue.IndexOf('[') != -1 && requestValue.IndexOf(']') != -1))
                                {
                                    flag = " LIKE ";
                                }

                                if (requestValue == "^")
                                {
                                    wheresql = string.Format("({0}='' OR {0} IS NULL)", searchfield);
                                    //不需要参数
                                    bindResult.Command.Parameters.RemoveAt(p.ParameterName);
                                }
                                else
                                {
                                    wheresql = string.Format("{0}{1}@{2}", searchfield, flag, p.ParameterName.TrimStart('@'));
                                    ((IDataParameter)bindResult.Command.Parameters[p.ParameterName]).Value = requestValue;
                                }
                            }
                        }
                        else//有WhereSQL的情况
                        {
                            if (muiltflag)
                            {
                                //说明为多行情况，带Value的，多行情况必须用In
                                if (p.Value.ToString().IndexOf(string.Format("({0})", p.ParameterName)) == -1)
                                {
                                    bindResult.ErrInfo = string.Format("DbAction【{0}】,当参数为多行并且指定WhereSQL时,参数需要写为:{1}", dbaction.Name, string.Format("({0})", p.ParameterName));
                                }
                                wheresql = p.Value.ToString().Replace(string.Format("({0})", p.ParameterName), string.Format("('{0}')", string.Join("','", values)));
                            }
                            else
                            {
                                wheresql = p.Value.ToString();
                                ((IDataParameter)bindResult.Command.Parameters[p.ParameterName]).Value = requestValue;
                            }

                        }

                        if (wheresql.Length > 0)
                            sbwhere.AppendFormat(" AND {0} ", wheresql);
                    }
                }
                if (dbaction.IsWhereFormat)
                {
                    bindResult.Command.CommandText = string.Format(bindResult.Command.CommandText, sbwhere.ToString());
                }
                else
                {
                    if (sbwhere.Length > 0)
                    {

                        if (dbaction.IsEndWithWhereSQL)
                        {
                            bindResult.Command.CommandText = string.Format("{0} WHERE {1}", bindResult.Command.CommandText,
                                sbwhere.ToString().TrimStart(" AND ".ToCharArray()));
                        }
                        else
                        {
                            bindResult.Command.CommandText = string.Format("{0} {1}", bindResult.Command.CommandText, sbwhere.ToString());
                        }
                    }
                }
                
                if (string.IsNullOrEmpty(dbaction.OrderBy) == false)
                {
                    bindResult.Command.CommandText = string.Format(@"{0}
{1}", bindResult.Command.CommandText, dbaction.OrderBy);
                }
                System.Diagnostics.Debug.WriteLine(string.Format("SearchCommandText:{0}", bindResult.Command.CommandText));
                #endregion
            }//处理Search参数结束
            else
            {
                #region//普通参数绑定
                System.Diagnostics.Debug.WriteLine(string.Format("Begin Bind Action:{0}", dbaction.Name));
                
                foreach (IDbDataParameter p in bindResult.Command.Parameters)
                {
                    if (BindParameter(getValueHanlder, p) == false)
                    {
                        string p_description = GetDescBySourceName(dbaction.ActionCommand, p.SourceColumn);
                        bindResult.EmptyKeys.Add(p_description);
                    }
                }
                #endregion
                if (dbaction.IsInit)    //假如为初始命令，系统自动添加PageID参数
                {
                    //添加必须的命令PageID
                    IDbDataParameter p_pageid = bindResult.Command.CreateParameter();
                    p_pageid.ParameterName = "@PageID";
                    p_pageid.Value = dbaction.Page.ID;
                    p_pageid.DbType = System.Data.DbType.Int32;
                    bindResult.Command.Parameters.Add(p_pageid);
                }
            }
           
            if (bindResult.IsOK)
            {
                //添加用户参数
                try
                {
                    IDbCommand cmmd = bindResult.Command;
                    

                    if (string.IsNullOrEmpty(cnnstr) == false && cmmd.Connection.State == ConnectionState.Closed)
                    {
                        cmmd.Connection.ConnectionString = cnnstr;
                    }
                    //添加全局参数
                    bindResult.AddGlobalParameter(userID);
                }
                catch (Exception err)
                {
                    bindResult.ErrInfo = string.Format("DbAction【{0}】添加@UserID参数出错:{1}", dbaction.Name, err.Message);
                }
            }

            return bindResult;
        }
        //public static BindBatchResult BindBatchAction(BatchDbAction batchdbaction, string detailData, ActionData mainKeyValueHandler)
        //{
        //    BindBatchResult result = new BindBatchResult();
        //    //result.CheckCommandBind 
        //    System.Diagnostics.Debug.WriteLine(string.Format("Begin Bind Action:{0}", batchdbaction.Name));
        //    result.CheckCommandBind.Command = batchdbaction.DbConfig.CloneCommand(batchdbaction.CheckCommand.Command);
        //    if (result.CheckCommandBind.Command != null)
        //    {
        //        BindParameters(result.CheckCommandBind.Command, mainKeyValueHandler, result, batchdbaction);
                
        //        //AddActionParameterHandler?.Invoke(dbaction, result.CheckCommandBind.Command, userID);
        //    }

        //    //处理数据
        //    //读取数据
        //    string data = detailData;
        //    if (string.IsNullOrEmpty(data) || data.Equals("[]"))
        //    {
        //        result.ErrInfo = "未取得Data数据.";
        //        return result;
        //    }
        //    ArrayList rowsD = data.ToObject<ArrayList>();

        //    foreach (Hashtable row in rowsD)
        //    {
        //        BindResult bindresult = new BindResult();

        //        bindresult.Command = batchdbaction.DbConfig.CloneCommand(batchdbaction.Command.Command);

        //        foreach (IDbDataParameter p in bindresult.Command.Parameters)
        //        {
        //            if (BindParameter(row, mainKeyValueHandler, p) == false)
        //            {
        //                string p_description = GetDescBySourceName(batchdbaction.Command, p.SourceColumn);
        //                bindresult.EmptyKeys.Add(p_description);
        //            }
        //        }
        //        result.DoCommandBinds.Add(bindresult);
        //    }
        //    result.ResultCommandBind.Command = batchdbaction.DbConfig.CloneCommand(batchdbaction.ResultCommand.Command);

        //    BindParameters(result.ResultCommandBind.Command, mainKeyValueHandler, result, batchdbaction);


        //    return result;
        //}
        internal static bool BindParameter(Hashtable table, ActionData mainKeyValueHandler, IDbDataParameter p)
        {
            string requestValue = string.Empty;
            if (table.ContainsKey(p.SourceColumn) == false)
            {
                requestValue = mainKeyValueHandler[p.SourceColumn];
                System.Diagnostics.Debug.WriteLine(string.Format("明细中不存在,采用RequestKey:【{0}】VALUE:【{1}】", p.SourceColumn, requestValue));
            }
            else
            {
                if (table[p.SourceColumn] == null)
                {
                    requestValue = null;
                }
                else
                {
                    requestValue = table[p.SourceColumn].ToString();
                }
                System.Diagnostics.Debug.WriteLine(string.Format("JSONData Key:【{0}】VALUE:【{1}】", p.SourceColumn, requestValue));
            }


            bool canempty = ((System.Data.Common.DbParameter)p).SourceColumnNullMapping;
            if (canempty == false && string.IsNullOrEmpty(requestValue))
            {
                bool validflag = false;
                if (requestValue == null)
                {
                    //说明前台未传值，假如有默认值，则使用默认值，若没有，则提示错误信息
                    validflag = p.Value != null;
                }
                if (validflag == false)
                {

                    System.Diagnostics.Debug.WriteLine(string.Format("Error:RequestKey【{0}】不允许为空!", p.SourceColumn));
                    return false;
                }
                else
                {
                    System.Diagnostics.Debug.WriteLine(string.Format("{0}采用默认值【{1}】", p.ParameterName, p.Value));
                }
            }
            else
            {
                if (string.IsNullOrEmpty(requestValue))
                {
                    p.Value = DBNull.Value;
                }
                else
                {
                    p.Value = requestValue;
                }
            }
            return true;
        }
        public static BindBatchResult BindBatchAction(BatchDbAction batchdbaction
            , ActionData valueHandler,int userID)
        {
            BindBatchResult result = new BindBatchResult();
            

            System.Diagnostics.Debug.WriteLine(string.Format("Begin Bind Action:{0}", batchdbaction.Name));
            result.CheckCommandBind.Command = batchdbaction.DbConfig.CloneCommand(batchdbaction.CheckCommand.Command);
            if (result.CheckCommandBind.Command != null)
            {
                BindParameters(result.CheckCommandBind.Command, valueHandler, result, batchdbaction);
            }

            ArrayList arrayList = valueHandler.GetArrayListValue("Data");
            foreach (Hashtable row in arrayList)
            {
                BindResult bindresult = new BindResult();

                bindresult.Command = batchdbaction.DbConfig.CloneCommand(batchdbaction.Command.Command);
                ActionData rowvaluehandler = new ActionData(row);

                foreach (IDbDataParameter p in bindresult.Command.Parameters)
                {
                    if (BindParameter(rowvaluehandler, p) == false)
                    {
                        string p_description = GetDescBySourceName(batchdbaction.Command, p.SourceColumn);
                        bindresult.EmptyKeys.Add(p_description);
                    }
                }

                result.DoCommandBinds.Add(bindresult);
            }
            result.ResultCommandBind.Command = batchdbaction.DbConfig.CloneCommand(batchdbaction.ResultCommand.Command);
            
            BindParameters(result.ResultCommandBind.Command, valueHandler, result, batchdbaction);

            //添加全局参数
            result.AddGlobalParameter(userID);
            return result;
        }
        internal static void BindParameters(IDbCommand cmmd, ActionData valueHandler, BindBatchResult result, BatchDbAction batchdbaction)
        {
            for (int i = 0; i < cmmd.Parameters.Count; i++)
            {
                IDbDataParameter p = (IDbDataParameter)cmmd.Parameters[i];
                if (BindParameter(valueHandler, p) == false)
                {
                    string p_description = GetDescBySourceName(batchdbaction.CheckCommand, p.SourceColumn);
                    result.CheckCommandBind.EmptyKeys.Add(p_description);
                }
            }
        }
        /// <summary>
        /// 将请求信息绑定到参数
        /// </summary>
        /// <param name="request"></param>
        /// <param name="p"></param>
        /// <returns>true--成功;false--失败</returns>
        internal static bool BindParameter(ActionData getValueHanlder, IDbDataParameter p)
        {
            string requestValue = getValueHanlder[p.SourceColumn];
            System.Diagnostics.Debug.WriteLine(string.Format("RequestKey:【{0}】VALUE:【{1}】", p.SourceColumn, requestValue));
            bool canempty = ((System.Data.Common.DbParameter)p).SourceColumnNullMapping;
            if (canempty == false && string.IsNullOrEmpty(requestValue))
            {
                bool validflag = false;
                if (string.IsNullOrEmpty(requestValue))
                {
                    //说明前台未传值，假如有默认值，则使用默认值，若没有，则提示错误信息
                    validflag = (p.Value != null && p.Value != DBNull.Value);
                }
                if (validflag == false)
                {

                    System.Diagnostics.Debug.WriteLine(string.Format("Error:RequestKey【{0}】不允许为空!", p.SourceColumn));
                    return false;
                }
                else
                {
                    System.Diagnostics.Debug.WriteLine(string.Format("{0}采用默认值【{1}】", p.ParameterName, p.Value));
                }
            }
            else
            {
                if (string.IsNullOrEmpty(requestValue))
                {
                    p.Value = DBNull.Value;
                    System.Diagnostics.Debug.WriteLine(string.Format("{0}为空，默认DBNULL", p.ParameterName));
                }
                else
                {
                    p.Value = requestValue;
                }

            }
            return true;
        }
        #endregion

        /// <summary>
        /// 根据字段，获取描述
        /// </summary>
        /// <param name="cmmd"></param>
        /// <param name="sourceColumn"></param>
        /// <returns></returns>
        static string GetDescBySourceName(ActionCommand cmmd ,string sourceColumn)
        {
            string p_description = cmmd.ParameterDescrtions[sourceColumn];

            if (string.IsNullOrEmpty(p_description))
            {
                p_description = sourceColumn;
            }
            return p_description;
        }


        internal static BindResult BindCommandParameters(DbCommand cmmd
            , ActionData keyvaluehandler
            , ActionCommand actionCommand
            ,DbConfig dbConfig)
        {
            BindResult result = new BindResult();

            result.Command = dbConfig.CloneCommand(cmmd);
            foreach (IDbDataParameter p in result.Command.Parameters)
            {
                if (BindParameter(keyvaluehandler, p) == false)
                {
                    string p_description = GetDescBySourceName(actionCommand, p.SourceColumn);
                    result.EmptyKeys.Add(p_description);
                }
            }
            return result;
        }
    }
}
