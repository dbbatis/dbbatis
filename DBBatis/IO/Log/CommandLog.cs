using System;
using System.Collections.Generic;
using System.Data;
using System.Text;
using DBBatis.Action;
using DBBatis.JSON;

namespace DBBatis.IO.Log
{
    /// <summary>
    /// 命令日志
    /// </summary>
    public class CommandLog
    {
        static short _CommandLogTempCount = 30;
        /// <summary>
        /// 日常命令数量 默认30
        /// </summary>
        public static short CommandLogTempCount
        {
            get { return _CommandLogTempCount; }
            set
            {
                _CommandLogTempCount = value;
            }

        }
        static private Queue<CommandLog> CommnadLogs = new Queue<CommandLog>();
        /// <summary>
        /// 构造函数
        /// </summary>
        /// <param name="tipInfo"></param>
        /// <param name="dbCommand"></param>
        /// <param name="result"></param>
        /// <param name="time"></param>
        public CommandLog(string tipInfo, IDbCommand dbCommand, object result, double time, DbConfig dbConfig)
        {
            TipInfo = tipInfo;
            DbCommand = dbCommand;
            Result = result;
            Time = time;
            DoTime = DateTime.Now;
            DbConfig = dbConfig;
        }
        public DbConfig DbConfig { get; set; }
        /// <summary>
        /// 提示信息
        /// </summary>
        public string TipInfo
        {
            get; set;
        }
        /// <summary>
        /// DB命令
        /// </summary>
        public IDbCommand DbCommand
        {
            get; set;
        }
        /// <summary>
        /// 执行命令
        /// </summary>
        public string CommandText
        {
            get; set;
        }
        /// <summary>
        /// 执行结果
        /// </summary>
        public object Result
        {
            get; set;
        }
        /// <summary>
        /// 执行时间
        /// </summary>
        public double Time
        {
            get; set;
        }
        /// <summary>
        /// 执行时间
        /// </summary>
        public DateTime DoTime
        {
            get; set;
        }
        public static string GetStrResult(object result)
        {
            if (result == null)
            {
                return string.Empty;
            }
            else
            {
                string josn = result.ToJson();
                return josn.Replace("},", string.Format("}},{0}", Environment.NewLine))
                    .Replace("],", string.Format("],{0}", Environment.NewLine))
                    .Replace(":[{", string.Format(":[{0}{{", Environment.NewLine));
            }

        }
        /// <summary>
        /// 记录Action
        /// </summary>
        /// <param name="action"></param>
        /// <param name="tipInfo">提示信息</param>
        /// <param name="dbCommand"></param>
        /// <param name="result"></param>
        /// <param name="beginTime"></param>
        public static void Log(DBBatis.Action.BatchDbAction action, string tipInfo, IDbCommand dbCommand, object result, DateTime beginTime)
        {
            Log(string.Format("PageID:{0} ActionName:{1} {2}", action.Page.ID, action.Name, tipInfo)
                , dbCommand, result, beginTime
                , action.DbConfig);
        }
        /// <summary>
        /// 记录Action
        /// </summary>
        /// <param name="action"></param>
        /// <param name="tipInfo">提示信息</param>
        /// <param name="dbCommand"></param>
        /// <param name="result"></param>
        /// <param name="beginTime"></param>
        public static void Log(DBBatis.Action.DbAction action, string tipInfo, IDbCommand dbCommand, object result, DateTime beginTime)
        {
            Log(string.Format("PageID:{0} ActionName:{1} {2}"
                , action.Page == null ? 0 : action.Page.ID, action.Name, tipInfo)
                , dbCommand, result, beginTime
                , action.DbConfig);
        }
        /// <summary>
        /// 记录Action
        /// </summary>
        /// <param name="action"></param>
        /// <param name="dbCommand"></param>
        /// <param name="result"></param>
        /// <param name="beginTime"></param>
        public static void Log(DBBatis.Action.DbAction action, IDbCommand dbCommand, object result, DateTime beginTime)
        {
            if (action.Page == null)
            {
                //说明为全局命令
                Log(string.Format("PageID:{0} ActionName:{1}", 0, action.Name)
                    , dbCommand, result, beginTime,action.DbConfig);
            }
            else
            {
                Log(string.Format("PageID:{0} ActionName:{1}", action.Page.ID, action.Name)
                    , dbCommand, result, beginTime
                    , action.DbConfig);
            }
        }
        /// <summary>
        /// 记录命令
        /// </summary>
        /// <param name="log"></param>
        public static void Log(string tipInfo, IDbCommand dbCommand, object result, DateTime beginTime, DbConfig dbConfig)
        {
            lock (CommnadLogs)
            {
                CommandLog log = new CommandLog(tipInfo, dbCommand, result, (DateTime.Now - beginTime).TotalSeconds, dbConfig);

                CommnadLogs.Enqueue(log);

                while (CommnadLogs.Count > CommandLogTempCount)
                {
                    CommnadLogs.Dequeue();
                }
            }
        }

        public static string GetLogs()
        {
            StringBuilder sb = new StringBuilder();
            sb.Append(@"
<head>
<script src='http://miniui.com/scripts/boot.js?ver=1' type='text/javascript'></script>

<style type='text/css'>
body{
background-color: #FEFEF2;
}
table.gridtable {
    font-family: verdana,arial,sans-serif;
    font-size:11px;
    color:#333333;
    border-width: 1px;
    border-color: #666666;
    border-collapse: collapse;
    margin-bottom:3px;
}
table.gridtable th {
    border-width: 1px;
    padding: 8px;
    border-style: solid;
    border-color: #666666;
    background-color: #FEFEF2;
}
table.gridtable td {
    border-width: 1px;
    padding: 8px;
    border-style: solid;
    border-color: #666666;
    background-color: #ffffff;
}
.mini-panel-body{
background-color: #FEFEF2;
}
.mini-panel-titleclick .mini-panel-header{
background-color: #FEFEF2;
color:blue;
}
textarea {

    outline: 0 none;
    border-color: rgba(82, 168, 236, 0.8);
    box-shadow: inset 0 1px 3px rgba(0, 0, 0, 0.1), 0 0 8px rgba(82, 168, 236, 0.6);
}
</style>
</head>
<body id='body' style='display:none;'>
");

            CommandLog[] templogs = CommnadLogs.ToArray();
            for (int i = templogs.Length - 1; i >= 0; i--)
            {
                CommandLog l = templogs[i];

                string title = string.Format("【{0}】时间:{1} 时长:{2} &nbsp;&nbsp;&nbsp;&nbsp;【{3}】", (i + 1).ToString().PadLeft(2, '0'), l.DoTime.ToString("HH:mm:ss"), l.Time.ToString().PadRight(9, '0'),
                    l.TipInfo);
                sb.AppendFormat(@"
<div class='mini-panel' title='{0}' iconCls='icon-node' style='width:100%;' 
    showToolbar='true' showCollapseButton='true'  allowResize='true' expanded='false'  collapseOnTitleClick='true'>", title);


                sb.AppendFormat("<textarea style='width:100%;height:300px;color:#3300ff;' spellcheck='false'>{0}</textarea>",
                   l.DbConfig.GetCommandString(l.DbCommand));
                //显示运行结果
                if (l.Result != null)
                {
                    Type resulttype = l.Result.GetType();
                    string htmltemp = string.Empty;
                    if (resulttype == typeof(DataTable))
                    {
                        htmltemp = GetTableHtml((DataTable)l.Result);
                        sb.Append(htmltemp);
                    }
                    else if (resulttype == typeof(DataSet))
                    {
                        htmltemp = GetTableHtml((DataSet)l.Result);
                        sb.Append(htmltemp);
                    }
                    else
                    {
                        sb.AppendFormat("<div>{0}</div>", l.Result);
                    }
                }
                else
                {
                    sb.AppendFormat("<div>{0}</div>", "此命令没有输出信息");
                }

                sb.Append("</div>");
            }


            sb.Append(@"
<script type='text/javascript'>
    mini.parse();$('#body').show();
</script>");
            return sb.ToString();
        }
        /// <summary>
        /// 获取字符串长度
        /// </summary>
        /// <param name="str"></param>
        /// <returns></returns>
        public static int GetLength(string str)
        {
            if (string.IsNullOrEmpty(str))
                return 0;
            ASCIIEncoding ascii = new ASCIIEncoding();
            int tempLen = 0;
            byte[] s = ascii.GetBytes(str);
            for (int i = 0; i < s.Length; i++)
            {
                if ((int)s[i] == 63)
                {
                    tempLen += 2;
                }
                else
                {
                    tempLen += 1;
                }
            }
            return tempLen;
        }
        public static string GetTableHtml(DataSet ds)
        {
            StringBuilder sb = new StringBuilder();
            foreach (DataTable dt in ds.Tables)
            {
                sb.Append(GetTableHtml(dt));
            }
            return sb.ToString();
        }

        public static string GetTableHtml(DataTable dt)
        {
            StringBuilder sbtable = new StringBuilder("<div style='max-height:200px;overflow:auto;'><table class=\"gridtable\"><tr>");

            foreach (DataColumn c in dt.Columns)
            {
                sbtable.AppendFormat("<th>{0}</th>", c.ColumnName);
            }
            sbtable.Append("</tr>");
            foreach (DataRow r in dt.Rows)
            {
                sbtable.Append("<tr>");
                foreach (DataColumn c in dt.Columns)
                {
                    sbtable.AppendFormat("<td>{0}</td>", r[c.ColumnName]);
                }
                sbtable.Append("</tr>");
            }
            sbtable.Append("</table></div>");
            return sbtable.ToString();
        }

    }

}
