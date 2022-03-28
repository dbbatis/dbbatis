using DBBatis.Action;
using System;
using System.Collections.Generic;
using System.Data;
using System.Linq;
using System.Text;

namespace DBBatis.IO.Log
{

    public class Log
    {
        internal static ErrorCommand LastErrorCommand = null;

        static readonly FileLog _Log = new FileLog(string.Format("{0}\\Log\\", AppDomain.CurrentDomain.BaseDirectory), "ErrLog.txt");
        /// <summary>
        /// 记录日志
        /// </summary>
        /// <param name="cmmd"></param>
        /// <param name="err"></param>
        /// <param name="dbConfig"></param>
        public static void WriteErrorCommand(IDbCommand cmmd, Exception err, DbConfig dbConfig)
        {
            Write(string.Empty, cmmd, err, dbConfig);
        }
        /// <summary>
        /// 记录日志
        /// </summary>
        /// <param name="TipInfo"></param>
        /// <param name="cmmd"></param>
        /// <param name="err"></param>
        public static void Write(string TipInfo, IDbCommand cmmd, Exception err, DbConfig dbConfig)
        {
            if (cmmd != null && dbConfig != null)
            {
                LastErrorCommand = new ErrorCommand(cmmd, err, dbConfig);
            }
            _Log.Write(TipInfo, cmmd, err,dbConfig);
        }
        /// <summary>
        /// 记录日志信息
        /// </summary>
        /// <param name="TipInfo">提示信息</param>
        /// <param name="err">错误信息</param>
        public static void Write(string TipInfo, Exception err)
        {
            Write(TipInfo, null, err, null);
        }
        /// <summary>
        /// 记录日志信息
        /// </summary>
        /// <param name="TipInfo">提示信息</param>
        /// <param name="errInfo">错误信息</param>
        public static void Write(string TipInfo, string errInfo)
        {
            System.Diagnostics.Debug.WriteLine(TipInfo);
            _Log.AddLogInfo(TipInfo, errInfo);
            System.Diagnostics.Debug.WriteLine(errInfo);
        }
    }
    /// <summary>
    /// 出错的命令
    /// </summary>
    internal class ErrorCommand
    {
        public ErrorCommand(IDbCommand cmmd, Exception err, DbConfig dbConfig)
        {
            DoTime = DateTime.Now;
            Command = cmmd;
            Err = err;
            DbConfig = dbConfig;
        }
        DbConfig DbConfig { get; set; }
        /// <summary>
        /// 出错的命令
        /// </summary>
        public IDbCommand Command { get; set; }
        /// <summary>
        /// 执行的时间
        /// </summary>
        public DateTime DoTime { get; set; }
        /// <summary>
        /// 错误信息
        /// </summary>
        public Exception Err { get; set; }
        /// <summary>
        /// 获取命令字符串
        /// </summary>
        /// <returns></returns>
        public string GetCommandString()
        {
            return DbConfig.GetCommandString(Command);
        }
        /// <summary>
        /// 获取错误日志
        /// </summary>
        /// <returns></returns>
        public static string GetCommandLogs()
        {
            StringBuilder sb = new StringBuilder();
            sb.AppendFormat(@"
<head>
<script src='js/boot.js?ver={0}' type='text/javascript'></script>
</head>
", DateTime.Now.Ticks);
            if (DbConfig.LastError == null)
            {
                sb.Append("<h3>未发现DataBase执行出错的命令.</h3>");
            }
            else
            {
                sb.AppendFormat("<h3>DataBase 错误时间:{0} 错误信息:{1}</h3>", DbConfig.LastErrorTime.ToString("yyyy-MM-dd HH:mm:ss"), DbConfig.LastError.Message);

                sb.AppendFormat(@"
<div id='panel1' class='mini-panel' title='{0}' iconCls='icon-node' style='width:100%;height:300px;' 
    showToolbar='true' showCollapseButton='true'  allowResize='true' expanded='false'  collapseOnTitleClick='true'>", "错误详细信息");
                sb.AppendFormat("<textarea style='width:100%;height:100%;color:blue;background-color: #FEFEF2;' spellcheck='false'>{0}</textarea>", DbConfig.LastError.ToString());
                sb.Append("</div>");
                sb.AppendFormat(@"
<div id='panel1' class='mini-panel' title='{0}' iconCls='icon-node' style='width:100%;height:400px;' 
    showToolbar='true' showCollapseButton='true'  allowResize='true' expanded='false'  collapseOnTitleClick='true'>", "SQL命令详细信息");
                sb.AppendFormat("<textarea style='width:100%;height:100%;color:blue;background-color: #FEFEF2;' spellcheck='false'>{0}</textarea>", DbConfig.LastErrorCommand);
                sb.Append("</div>");

            }

            if (Log.LastErrorCommand == null)
            {
                sb.Append("<h3>未发现Action执行出错的命令</h3>");
            }
            else
            {
                sb.AppendFormat("<h3>Action 错误时间:{0} 错误信息:{1}</h3>", Log.LastErrorCommand.DoTime.ToString("HH:mm:ss"), Log.LastErrorCommand.Err.Message);

                sb.AppendFormat(@"
<div id='panel1' class='mini-panel' title='{0}' iconCls='icon-node' style='width:100%;height:300px;' 
    showToolbar='true' showCollapseButton='true'  allowResize='true' expanded='false'  collapseOnTitleClick='true'>", "错误详细信息");
                sb.AppendFormat("<textarea style='width:100%;height:100%;color:blue;background-color: #FEFEF2;' spellcheck='false'>{0}</textarea>", Log.LastErrorCommand.Err.ToString());
                sb.Append("</div>");

                sb.AppendFormat(@"
<div id='panel1' class='mini-panel' title='{0}' iconCls='icon-node' style='width:100%;height:400px;' 
    showToolbar='true' showCollapseButton='true'  allowResize='true' expanded='false'  collapseOnTitleClick='true'>", "SQL命令详细信息");
                sb.AppendFormat("<textarea style='width:100%;height:100%;color:blue;background-color: #FEFEF2;' spellcheck='false'>{0}</textarea>"
                    , Log.LastErrorCommand.DbConfig.GetCommandString(Log.LastErrorCommand.Command));
                sb.Append("</div>");
            }
            sb.AppendLine();
            sb.Append(@"
<script>type='text/javascript'>
    mini.parse();
    $('.mini-textarea').each(function () {
        var el = mini.get($(this).context.id).getTextEl();
        $(el).attr('spellcheck', false)
    });
</script>");
            return sb.ToString();
        }
    }
}
