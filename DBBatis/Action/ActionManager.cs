using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Data;
using System.Data.Common;
using System.IO;
using System.Linq;
using System.Text;
using System.Collections;
using DBBatis.JSON;
using DBBatis.IO.Log;
using DBBatis.Security;
using DBBatis.Web;
using System.Reflection;

namespace DBBatis.Action
{
    /// <summary>
    /// Action管理类
    /// </summary>
    public class ActionManager
    {
        internal static Dictionary<int, Page> _Pages = new Dictionary<int, Page>();
        internal static FileLog ErrLog = IO.Log.Logs.GetLog("DBBatis.Err");
        /// <summary>
        /// DbAction集合
        /// </summary>
        public static GloblaDbAction GloblaDbAction { get; set; }

        public static bool IsInit = false;
        /// <summary>
        /// 移除Page,若为0，移除所有
        /// </summary>
        /// <param name="page"></param>
        public static void RemovePage(int page)
        {
            lock (_Pages)
            {
                if (page <= 0)
                {
                    _Pages.Clear();
                }
                else
                {
                    if(_Pages.ContainsKey(page))
                    {
                        _Pages.Remove(page);
                    }
                }
            }
             
        }
        /// <summary>
        /// 构造函数
        /// </summary>
        private ActionManager()
        {
            lock (_Pages)
            {
                if (IsInit == false || MainConfig.IsNeedUpdate)
                {
                    //加载数据库信息
                    if (string.IsNullOrEmpty(MainConfig.MainConfigPath) == false)
                    {
                        MainConfig.InitMainConfig(MainConfig.MainConfigPath);
                        GloblaDbAction = GloblaDbAction.InitGlobalDbAction(MainConfig.GlobalDbActionPath);
                        IsInit = true;
                        MainConfig.IsNeedUpdate = false;
                    }
                }
            }
        }

        /// <summary>
        /// 单件实例
        /// </summary>
        /// <returns></returns>
        public static ActionManager Instance()
        {
            return new ActionManager();
        }


        #region//公用方法
        /// <summary>
        /// 获取PageConfig
        /// </summary>
        /// <param name="pageID"></param>
        /// <param name="actionName"></param>
        /// <returns></returns>
        public Page GetPage(short pageID)
        {
            System.Diagnostics.Debug.WriteLine(string.Format("Pages:{0}", _Pages.Count));
            Page p = null;
            lock (_Pages)
            {
                if (_Pages.ContainsKey(pageID) == false)
                {
                    string temppath = string.Format("{0}/{1}.page", MainConfig.PagePath, pageID);
                    System.Diagnostics.Debug.WriteLine(string.Format("Page:{0}", temppath));
                    if (System.IO.File.Exists(temppath))
                    {
                        p = Page.InitPage(temppath);

                        System.Diagnostics.Debug.Assert(p.ID == pageID, string.Format("文件{0}的ID与文件名不一致.", temppath));

                        if (p.ID == pageID && _Pages.ContainsKey(pageID) == false)
                        {
                            _Pages.Add(p.ID, p);
                        }
                    }
                    else
                    {
                        throw new FileNotFoundException("未找到Page文件.", temppath);
                    }

                }
                else
                {
                    p = _Pages[pageID];
                }
            }
            return p;
        }
        public ActionResult DoDbActions(Dictionary<string, string> keyValues, int userID,
             string connectionString)
        {
            ActionData handlerValue = new ActionData(keyValues);
            return DoDbActions(handlerValue, userID,  connectionString);
        }

        public ActionResult DoDbActions( ActionData handlerValue, int userID,
             string connectionString)
        {
            ActionResult result = new ActionResult();
            
            string pageid = handlerValue["PageID"];
            string actionName = handlerValue["ActionName"];
            if (string.IsNullOrEmpty(actionName))
            {
                result.ErrMessage = "未指定ActionName";
                return result;
            }
            DbAction dbAction;
            if (string.IsNullOrEmpty(pageid))
            {
                if (GloblaDbAction.DbActions.ContainsKey(actionName) == false)
                {
                    result.ErrMessage = string.Format("Invalid GlobalDbAction [{0}] ", actionName);
                    return result;
                }
                else
                {
                    dbAction = GloblaDbAction.DbActions[actionName];
                    return dbAction.Do(handlerValue, userID,  connectionString);
                }
            }
            else
            {
                short pid = 0;
                if (short.TryParse(pageid, out pid))
                {
                    Page page = GetPage(pid);
                    if (page == null)
                    {
                        result.ErrMessage = string.Format("未找到配置PageID为{0}的Page文件", pid);
                        return result;
                    }
                    if (page.DbActions.ContainsKey(actionName))
                    {
                        dbAction = page.GetAction(actionName);
                        return dbAction.Do(handlerValue, userID, connectionString);
                        
                    }else if (page.BatchDbActions.ContainsKey(actionName))
                    {
                        BatchDbAction batchDbAction = page.BatchDbActions[actionName];
                        return batchDbAction.Do(handlerValue,userID,connectionString);
                    }
                    else
                    {
                        if (actionName.Equals("Save", StringComparison.CurrentCultureIgnoreCase))
                        {
                            //假如为保存命令
                            Hashtable hashtable = handlerValue.GetHashTableValue("Data");
                            ActionData mainhandlervalue = new ActionData(hashtable);
                            string keyvalue = mainhandlervalue[page.PKField];
                            if (keyvalue == null)
                            {
                                result.ErrMessage = string.Format("提交数据中，必须提供主键[{0}]值", page.PKField);
                                return result;
                            }
                            else if (keyvalue.Length == 0)
                            {
                                //新增，调取新增命令
                                dbAction = page.DbActions["Insert"];
                            }
                            else
                            {
                                dbAction = page.DbActions["Update"];
                            }
                            return dbAction.Do(handlerValue, userID, connectionString);
                        }
                        result.ErrMessage = string.Format("{0}.Page中未找到Action:{1}", pid,actionName);
                        return result;
                    }
                    

                }
                else
                {
                    result.ErrMessage = string.Format("PageID[{0}]必须为整型数字.", pageid);
                }
            }
            
            return result;
        }


       
        #endregion


        #region//内部方法
        /// <summary>
        /// 处理加载方法返回的委托
        /// </summary>
        /// <param name="action">当前加载Action</param>
        /// <param name="actionResult">Action返回的信息</param>
        public delegate void InitActionResultHandler(DbAction action, ActionResult actionResult);
        /// <summary>
        /// 处理加载方法返回的信息
        /// </summary>
        public static InitActionResultHandler HandlerInitActionResult;

        public delegate bool CheckPopedomHandler(DbAction action, ActionData keyvalueHandler);
        /// <summary>
        /// 检查是否有权限
        /// </summary>
        public static CheckPopedomHandler HandlerCheckPopedom { get; set; }


        #endregion

        #region//工作流相关
        ///// <summary>
        ///// 格式化工作流命令
        ///// </summary>
        ///// <param name="action">工作流Action</param>
        ///// <param name="cmmd"></param>
        ///// <param name="result"></param>
        ///// <returns></returns>
        //internal static bool HandlerFlowFormatTableCommand(DbAction action, DbCommand cmmd, ref ActionResult result)
        //{
        //    if (cmmd.Parameters.Contains("@TableName") == false)
        //    {
        //        result.ErrMessage = "命令参数中必须提供TableName参数";
        //        return false;
        //    }
        //    cmmd.CommandText = string.Format(cmmd.CommandText, cmmd.Parameters["@TableName"].Value);
        //    return true;
        //}
        //private ActionResult GetNextNodeByTableName(DbAction action, string tableName, string currentOPNodeID, int userid,
        //    int billid, string opFlag)
        //{
        //    ActionResult flowresult = null;

        //    Dictionary<string, string> nvs = new Dictionary<string, string>();
        //    HandlerValue keyvaluehandler = null;
        //    ActionResult result = new ActionResult();
        //    //增加流程ID和初始状态
        //    DbAction flowgetNextNode = GloblaDbAction.DbActions["Flow_GetNextNode"];

        //    nvs = new Dictionary<string, string>();
        //    nvs.Add("TableName", tableName);
        //    nvs.Add("BILLID", billid.ToString());
        //    nvs.Add("OPFlag", opFlag);

        //    keyvaluehandler = new HandlerValue(nvs);


        //    flowresult = DoDbAction(flowgetNextNode, keyvaluehandler, HandlerFlowFormatTableCommand, userid, action.DbConfig.ConnectionString, null);


        //    if (flowresult.IsOK == false)
        //    {
        //        return flowresult;
        //    }
        //    DataTable flowtypedt = (DataTable)flowresult.Data;
        //    if (flowtypedt.Rows.Count != 1)
        //    {
        //        result.Data = flowresult.Data;
        //        result.ErrMessage = "未取得正确的工作流ID,请与管理员联系.";
        //    }
        //    else
        //    {
        //        nvs = new Dictionary<string, string>();
        //        nvs.Add("FlowTypeID", flowtypedt.Rows[0]["FlowTypeID"].ToString());
        //        string dbCurrentOPNodeID = flowtypedt.Rows[0]["CurrentOPNode"].ToString();

        //        string BeforeAction = flowtypedt.Rows[0]["BeforeAction"].ToString();
        //        string AfterAction = flowtypedt.Rows[0]["AfterAction"].ToString();

        //        string NextNode = flowtypedt.Rows[0]["NextNode"].ToString();
        //        bool ReceiveIsNodeUser = (bool)flowtypedt.Rows[0]["ReceiveIsNodeUser"];
        //        //检查单据中的状态与前端传入的是否一致
        //        if (dbCurrentOPNodeID != currentOPNodeID)
        //        {
        //            result.ErrMessage = "数据状态发生变化,请重新打开单据.";
        //            return result;
        //        }

        //        nvs.Add("NextNodeID", NextNode);
        //        nvs.Add("ReceiveIsNodeUser", ReceiveIsNodeUser ? "1" : "0");
        //        nvs.Add("CurrentNode", flowtypedt.Rows[0]["CurrentNode"].ToString());
        //        nvs.Add("TableName", tableName);

        //        nvs.Add("BeforeAction", BeforeAction);
        //        nvs.Add("AfterAction", AfterAction);
        //        nvs.Add("PageID", flowtypedt.Rows[0]["PageID"].ToString()); 


        //        if (string.IsNullOrEmpty(NextNode))
        //        {
        //            result.ErrMessage = "未取得下一节点流程ID,请与管理员联系.";
        //        }
        //        else
        //        {
        //            result.Data = nvs;
        //        }
        //    }
        //    return result;
        //}
        ///// <summary>
        ///// 获取下一节点信息
        ///// </summary>
        ///// <param name="action"></param>
        ///// <param name="flowTypeID">流程ID</param>
        ///// <param name="currentOPNodeID">当前操作传入ID</param>
        ///// <param name="userID"></param>
        ///// <param name="billid">当前单据ID</param>
        ///// <param name="opFlag">0-执行1-拒绝0-取消</param>
        ///// <returns></returns>
        //private ActionResult GetNextNode(DbAction action,int flowTypeID, string currentOPNodeID, int userid, int billid, string opFlag)
        //{
        //    ActionResult flowresult = null;
        //    string tableName = string.Empty;

        //    Dictionary<string, string> nvs = new Dictionary<string, string>();
        //    HandlerValue keyvaluehandler = null;
        //    if (action.Page != null)
        //    {
        //        tableName = action.Page.Name;
        //    }
        //    else
        //    {
        //        DbAction getflowtablename = GloblaDbAction.DbActions["Flow_GetFlowTableName"];
        //        nvs.Add("FlowTypeID", flowTypeID.ToString());
        //        keyvaluehandler = new HandlerValue(nvs);

        //        flowresult = DoDbAction(getflowtablename, keyvaluehandler,null, userid, action.DbConfig.ConnectionString, null);
        //        if (flowresult.IsOK == false)
        //        {
        //            return flowresult;
        //        }
        //        tableName = flowresult.Data.ToString();
        //        if (string.IsNullOrEmpty(tableName))
        //        {
        //            flowresult.ErrMessage = string.Format("未找到[{0}]对应的工作流信息", nvs["FlowTypeID"]);
        //            return flowresult;
        //        }
        //    }


        //    return GetNextNodeByTableName(action, tableName, currentOPNodeID, userid, billid, opFlag);
        //}
        #endregion


        #region//加载特殊处理方法
        //internal static Dictionary<short, Dictionary<string, ActionHandlerBase>> PageHandlers = new Dictionary<short, Dictionary<string, ActionHandlerBase>>();
        //internal static Dictionary<string, ActionHandlerBase> GlobalHanlers = new Dictionary<string, ActionHandlerBase>();
        //private void LoadActionHandler()
        //{
        //    lock (this)
        //    {
        //        PageHandlers = new Dictionary<short, Dictionary<string, ActionHandlerBase>>();
        //           GlobalHanlers = new Dictionary<string, ActionHandlerBase>();
        //        string[] files = System.IO.Directory.GetFiles(
        //        AppDomain.CurrentDomain.BaseDirectory, "*.dll", SearchOption.AllDirectories);
        //        System.Collections.ObjectModel.Collection<Assembly> array = new System.Collections.ObjectModel.Collection<Assembly>();
        //        foreach (string f in files)
        //        {
        //            try
        //            {
        //                array.Add(Assembly.LoadFrom(f));
        //            }
        //            catch (Exception err)
        //            {

        //            }
        //        }
        //        Collection<ActionHandlerBase> Handlers = new Collection<ActionHandlerBase>();
        //        for (int j = 0; j < array.Count; j++)
        //        {
        //            try
        //            {
        //                Assembly assembly = array[j];
        //                Type[] array2 = assembly.GetTypes();
        //                for (int k = 0; k < array2.Length; k++)
        //                {
        //                    Type T2 = array2[k];
        //                    if (T2.BaseType == typeof(ActionHandlerBase))
        //                    {
        //                        Type[] types = new Type[0];

        //                        ActionHandlerBase service = (ActionHandlerBase)T2.GetConstructor(types)
        //                            .Invoke(new string[] { });
        //                        if (service.PageID != 0)
        //                        {
        //                            if (PageHandlers.ContainsKey(service.PageID))
        //                            {
        //                                PageHandlers[service.PageID].Add(service.ActionName, service);
        //                            }
        //                            else
        //                            {
        //                                PageHandlers.Add(service.PageID, new Dictionary<string, ActionHandlerBase>()
        //                                {
        //                                    {service.ActionName, service},
        //                                });
        //                            }
        //                        }
        //                        else
        //                        {
        //                            GlobalHanlers.Add(service.ActionName, service);
        //                        }
        //                    }
        //                }
        //            }
        //            catch (Exception err) { }

        //        }
        //    }
            
        //}
        #endregion
    }
    internal class PageHandlers
        : Dictionary<short, ActionHandlers>
    {
    }
    internal class ActionHandlers
        : KeyedCollection<string, ActionHandlerBase>
    {
        protected override string GetKeyForItem(ActionHandlerBase item)
        {
            return item.ActionName;
        }
    }

}
