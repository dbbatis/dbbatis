using System;
using System.Text;
using DBBatis.JSON;
using DBBatis.Action;
using DBBatis.IO.Compression;
using System.Reflection;
using System.Threading.Tasks;
using DBBatis.IO.Log;

namespace DBBatis.Web
{

    public abstract class HandlerContextBase
    {
        /// 当长度超过时，则压缩
        /// </summary>
        public static int GZIPLenth { get; set; }
        /// <summary>
        /// 全局处理命令
        /// </summary>
        public static HandlerCommand GlobalHandlerCommand { get; set; }
        /// <summary>
        /// 检查是否有效
        /// </summary>
        /// <returns></returns>
        public abstract bool CheckValid();
        /// <summary>
        /// 当前路径
        /// </summary>
        /// <returns></returns>
        public abstract string GetPath();
        /// <summary>
        /// 当前路径
        /// </summary>
        /// <returns></returns>
        public abstract string RawUrl
        {
            get;
        }
        /// <summary>
        /// 当前Session
        /// </summary>
        public abstract object Session { get; }
        /// <summary>
        /// 设置Session
        /// </summary>
        /// <param name="key">Key</param>
        /// <param name="value">值</param>
        public abstract void SetSession(string key, object value);

        public abstract object GetSession(string key);
        /// <summary>
        /// 当前链接，空则使用默认
        /// </summary>
        public string ConnectionString { get; set; }
        public DbConfig GetDbConfig()
        {
            DbConfig dbConfig = MainConfig.GetDbConfig();
            if (string.IsNullOrEmpty(ConnectionString) == false)
            {

                dbConfig = DbConfig.GetDbConfig(dbConfig.DbType, ConnectionString);
            }
            return dbConfig;
        }
        /// <summary>
        /// 当前用户ID
        /// </summary>
        public abstract int UserID
        {
            get;
        }
        public HandlerCommand HandlerCommand { get; set; }
        public abstract ActionData Data { get; set; }
        /// <summary>
        /// 是否支持GZIP
        /// </summary>
        public abstract bool CanGZIP { get; }
        /// <summary>
        /// 执行Action
        /// </summary>
        /// <returns></returns>
        public ActionResult DoDbActions()
        {
            return ActionManager.Instance().DoDbActions(this.Data
                 , this.UserID, HandlerCommand, ConnectionString);
        }
        /// <summary>
        /// 执行指定类型实例中的方法，方法名取HandlerValue["Method"]
        /// </summary>
        /// <param name="type">指定类型</param>
        /// <param name="objects">方法中传入的参数</param>
        public void DoMethod(object o, object[] objects)
        {
            ActionResult result = new ActionResult();
            string methodname = this.Data["Method"];
            if (string.IsNullOrEmpty(methodname))
            {
                result.ErrMessage = "请指定的Method";
            }
            else
            {
                MethodInfo method = o.GetType().GetMethod(methodname, BindingFlags.Public | BindingFlags.NonPublic
                    | BindingFlags.Instance | BindingFlags.Static);

                if (method == null)
                {
                    result.ErrMessage = string.Format("无效的Method:{0}", methodname);
                }
                else
                {
                    result = (ActionResult)method.Invoke(o, objects);
                }
            }
            this.Write(result);

        }
        public virtual Task WriteHtml(string value)
        {
            byte[] bytes = Encoding.UTF8.GetBytes(value);
            return WriteCompressData(bytes, "text/html;charset=utf-8");
        }
        /// <summary>
        /// 写入JSON数据
        /// </summary>
        /// <param name="value"></param>
        /// <returns></returns>
        public virtual Task Write(object o)
        {
            string value = o.ToJson();
            byte[] bytes = Encoding.UTF8.GetBytes(value);
            return WriteCompressData(bytes, "application/json;charset=utf-8");
        }
        /// <summary>
        /// 写入JSON数据
        /// </summary>
        /// <param name="value"></param>
        /// <returns></returns>
        public virtual Task Write(string value)
        {
            byte[] bytes = Encoding.UTF8.GetBytes(value);
            return WriteCompressData(bytes, "application/json;charset=utf-8");
        }
        public Task Write(string value, string contentType)
        {
            byte[] bytes = Encoding.UTF8.GetBytes(value);
            return WriteCompressData(bytes, contentType);
        }
        /// <summary>
        /// 
        /// </summary>
        /// <param name="bytes"></param>
        /// <param name="contentType"></param>
        /// <returns></returns>
        public Task WriteCompressData(byte[] bytes, string contentType)
        {
            bool gzip = false;
            if (CanGZIP && bytes.Length > GZIPLenth)
            {
                bytes =  Zip.GZipCompress(bytes);
                gzip = true;
            }
            return Write(bytes, contentType, gzip);
        }
        /// <summary>
        /// 
        /// </summary>
        /// <param name="bytes"></param>
        /// <param name="contentType"></param>
        /// <param name="gzip">标识字节是否已经压缩过</param>
        /// <returns></returns>
        public abstract Task Write(byte[] bytes, string contentType, bool gzip);



        public Task Precess()
        {
            if (!CheckValid()) return Task.CompletedTask;
            string path = GetPath();
            string[] temps = path.Split('/');
            string endname = temps[temps.Length - 1].ToLower();
            endname = endname.Substring(0, endname.IndexOf('.'));
#if DEBUG
            if (endname.EndsWith("test"))
            {
                TestActionHandler();//处理测试
                return Task.CompletedTask; ;
            }
            else if (endname.EndsWith("testp"))
            {
                PTestActionHandler();
                return Task.CompletedTask;
            }

            else if (endname.EndsWith("listaction"))
            {
                return ListActionHandler();

            }
            else if (endname.EndsWith("cnn"))
            {

                return Write(MainConfig.GetDbConfig().ConnectionString);
            }
            else if (endname.EndsWith("url"))
            {
                Uri uri = new Uri(RawUrl);

                return Write(string.Format("{0}:{1}", uri.Host, uri.Port));

            }
            else if (endname.EndsWith("p"))
            {
                //解析命令参数
                return PActionHandler();

            }
            else if (endname.EndsWith("log"))
            {
                return WriteHtml(CommandLog.GetLogs());
            }
            else if (endname.EndsWith("error", StringComparison.CurrentCultureIgnoreCase))
            {
                string v = ErrorCommand.GetCommandLogs();
                return WriteHtml(v);
            }
#endif
            if (endname.EndsWith("combobox"))
            {
                ComboBoxManager comboBox = new ComboBoxManager(this);
                comboBox.Process();
                return Task.CompletedTask;
            }
            ActionResult result = new ActionResult();
            if (endname.EndsWith("reload", StringComparison.CurrentCultureIgnoreCase))
            {
                lock (ActionManager._Pages)
                {
                    ActionManager._Pages.Clear();
                    lock (ActionManager.GloblaDbAction)
                    {
                        ActionManager.GloblaDbAction = GloblaDbAction.InitGlobalDbAction(MainConfig.GlobalDbActionPath);
                    }
                }

                this.Write("操作成功.");
            }
            else
            {
                result = ActionManager.Instance().DoDbActions(Data, UserID, HandlerCommand, ConnectionString); ;
            }

            return Write(result.ToJson());
        }
#if DEBUG
        /// <summary>
        /// 处理测试SQL
        /// </summary>
        /// <param name="context"></param>
        private Task TestActionHandler()
        {
            string pageid = Data["PageID"];
            string actionname = Data["ActionName"];
            short pageidint;
            if (string.IsNullOrEmpty(pageid) || short.TryParse(pageid, out pageidint) == false)
            {
                return Write("获取Action模拟命令,请指定参数:PageID和ActionName");
            }
            else
            {
                string writevalue = string.Empty;
                Page pc = ActionManager.Instance().GetPage(pageidint);
                if (pc == null)
                {
                    return Write("系统未取得Page信息,请确认PageID是否正确。");
                }
                if (string.IsNullOrEmpty(actionname))
                {
                    return WriteActionList(pc);
                }
                else
                {
                    if (pc.DbActions.ContainsKey(actionname))
                    {
                        writevalue = pc.DbActions[actionname].GetTestSQLCommand();

                    }
                    else if (pc.BatchDbActions.ContainsKey(actionname))
                    {
                        writevalue = pc.BatchDbActions[actionname].GetTestSQLCommand();
                    }
                    else
                    {
                        writevalue = string.Format("在PageID【{0}】中未找到命令【{1}】，注意命令区分大小写。", pageidint, actionname);

                    }
                    return WriteHtml(string.Format("<textarea spellcheck='false' style='height:100%; width:100%'>{0}</textarea>", writevalue));
                }

            }
        }
        /// <summary>
        /// 列出命令信息
        /// </summary>
        /// <param name="pc"></param>
        private Task WriteActionList(Page pc)
        {
            StringBuilder sb = new StringBuilder();
            if (pc.DbActions != null && pc.DbActions.Count > 0)
            {
                sb.Append("<h5>以下为DBAction</h5><ol>");

                foreach (string actionName in pc.DbActions.Keys)
                {
                    sb.AppendFormat("<li><a href=\"{0}&ActionName={1}\">【{1}】</a>{2}</li>", RawUrl, actionName, pc.DbActions[actionName].Description);
                }
                sb.Append("</ol>");

            }
            if (pc.BatchDbActions != null && pc.BatchDbActions.Count > 0)
            {
                sb.Append("<h5>以下为BatchDbAction</h5><ol>");
                foreach (string actionName in pc.BatchDbActions.Keys)
                {

                    sb.AppendFormat("<li><a href=\"{0}&ActionName={1}\">【{1}】</a>{2}</li>", RawUrl, actionName, pc.BatchDbActions[actionName].Description);
                }
                sb.Append("</ol>");


            }
            return WriteHtml(sb.ToString());
        }

        /// <summary>
        /// p.action 参数处理相关方法
        /// </summary>
        /// <param name="context"></param>
        private Task PTestActionHandler()
        {
            string pageid = Data["PageID"];
            string actionname = Data["ActionName"];
            short pageidint ;
            ActionResult result = new ActionResult();

            if (string.IsNullOrEmpty(pageid) || short.TryParse(pageid, out pageidint) == false || string.IsNullOrEmpty(actionname))
            {
                result.ErrMessage = "获取Action参数,请指定参数:PageID和ActionName";

            }
            else
            {
                string writevalue = string.Empty;
                Page pc = ActionManager.Instance().GetPage(pageidint);
                if (pc == null)
                {
                    result.ErrMessage = "系统未取得Page信息,请确认PageID是否正确。";
                }
                else
                {
                    if (pc.DbActions.ContainsKey(actionname))
                    {
                        System.Collections.Specialized.NameValueCollection nvs = new System.Collections.Specialized.NameValueCollection();
                        ActionCommand actionCommand = pc.DbActions[actionname].ActionCommand;

                        StringBuilder sb = new StringBuilder(string.Format(@"
{{
PageID:{0}
,ActionName:'{1}'
", pageid, actionname));

                        for (int i = 0; i < actionCommand.Command.Parameters.Count; i++)
                        {
                            System.Data.Common.DbParameter p = actionCommand.Command.Parameters[i];
                            string desc = actionCommand.ParameterDescrtions[p.SourceColumn];
                            if (string.IsNullOrEmpty(desc))
                            {
                                desc = p.SourceColumn;
                            }
                            sb.AppendLine(string.Format(",{0}:''   //{1}", p.SourceColumn, desc));
                        }
                        sb.Append("}");
                        result.Data = sb.ToString();
                    }
                    else if (pc.BatchDbActions.ContainsKey(actionname))
                    {
                        result.Data = "暂未处理";
                    }
                    else
                    {
                        result.ErrMessage = string.Format("在PageID【{0}】中未找到命令【{1}】，注意命令区分大小写。", pageidint, actionname);

                    }

                }

            }
            return Write(result.ToJson());
        }

        private Task ListActionHandler()
        {
            //
            string pageid = Data["PageID"];
            ActionResult result = new ActionResult();
            short pageidshort = 0;
            if (string.IsNullOrEmpty(pageid) || short.TryParse(pageid, out pageidshort) == false)
            {
                result.ErrMessage = "请指定PageID,DBFlag若不指定，系统将采用第一个数据连接。";
            }
            //else if (MainConfig.DbConfigs == null || MainConfig.DbConfigs.Count == 0)
            //{
            //    result.ErrMessage = "请指定指定主配置文件。";
            //}

            if (!result.IsErr)
            {
                Page pc = ActionManager.Instance().GetPage(pageidshort);
                if (pc == null)
                {
                    result.ErrMessage = "系统未取得Page信息,请确认PageID是否正确。";
                }
                else
                {
                    System.Data.DataTable dt = new System.Data.DataTable();
                    dt.Columns.Add("命令");
                    dt.Columns.Add("描述");
                    dt.Columns.Add("返回类型");
                    dt.Columns.Add("返回描述");
                    foreach (string key in pc.DbActions.Keys)
                    {
                        System.Data.DataRow row = dt.NewRow();
                        DbAction dbAction = pc.DbActions[key];
                        row[0] = dbAction.Name;
                        row[1] = dbAction.Description;
                        row[2] = dbAction.Result.Simple.ToString();
                        row[3] = dbAction.Result.Description;
                        dt.Rows.Add(row);
                    }
                    result.Data = dt;
                }
            }

            return Write(result.ToJson());
        }
        /// <summary>
        /// p.action 参数处理相关方法
        /// </summary>
        /// <param name="context"></param>
        private Task PActionHandler()
        {
            string pageid = Data["PageID"];
            string actionname = Data["ActionName"];
            short pageidint;
            if (string.IsNullOrEmpty(pageid) || short.TryParse(pageid, out pageidint) == false)
            {
                return WriteHtml("获取Action参数,请指定参数:PageID和ActionName");
            }
            else
            {
                string writevalue = string.Empty;
                Page pc = ActionManager.Instance().GetPage(pageidint);
                if (pc == null)
                {
                    return WriteHtml("系统未取得Page信息,请确认PageID是否正确。");
                }
                if (string.IsNullOrEmpty(actionname))
                {
                    return WriteActionList(pc);
                }
                else
                {
                    if (pc.DbActions.ContainsKey(actionname))
                    {
                        writevalue = pc.DbActions[actionname].GetXMLParameterElements();

                    }
                    else if (pc.BatchDbActions.ContainsKey(actionname))
                    {
                        writevalue = pc.BatchDbActions[actionname].GetXMLParameterElements();
                    }
                    else
                    {
                        writevalue = string.Format("在PageID【{0}】中未找到命令【{1}】，注意命令区分大小写。", pageidint, actionname);

                    }
                    return WriteHtml(string.Format("<textarea spellcheck='false' style='height:100%; width:100%'>{0}</textarea>", writevalue));
                }

            }
        }
#endif
    }
}
