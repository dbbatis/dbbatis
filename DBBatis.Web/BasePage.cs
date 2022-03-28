using DBBatis.Action;
using DBBatis.IO.Compression;
using DBBatis.IO.Log;
using System;
using System.Data.Common;
using System.Web;
using System.Web.SessionState;

namespace DBBatis.Web
{
    /// <summary>
    /// 获取当前DB用户委托
    /// </summary>
    /// <param name="session">当前会话Session</param>
    /// <returns></returns>
    public delegate DbConfig GetDBBySessionHandler(HttpSessionState session);
    /// <summary>
    /// 设置当前用户信息
    /// </summary>
    /// <param name="context">当前会话</param>
    /// <param name="connectionString">连接字符串</param>
    public delegate void SetConnectionStringHandler(HttpContext context, string connectionString);

    public class BasePage : System.Web.UI.Page
    {
        /// <summary>
        /// 根据Session，获取DB信息委托，假如未指定，系统采用默认DB
        /// </summary>
        public static GetDBBySessionHandler GetDbConfigBySession
        {
            get;
            set;
        }
        /// <summary>
        /// 设置用户信息委托
        /// </summary>
        public static SetConnectionStringHandler SetConnectionStringHandler;
        /// <summary>
        /// 检查Referer有效性
        /// </summary>
        private bool _CheckReferer = true;
        private bool _CheckIsOut = true;
        public bool IsNeedCheckOut
        {
            get { return _CheckIsOut; }
            protected set { _CheckIsOut = value; }
        }
        /// <summary>
        /// 检查Referer有效性
        /// </summary>
        public bool CheckReferer
        {
            get
            {
                return _CheckReferer;
            }
          protected  set
            {
                _CheckReferer = value;
            }
        }

        protected override void OnInitComplete(EventArgs e)
        {
            if (IsNeedCheckOut)
            {
                if (BasePage.IsOut(this.Context)) return;
            }
            base.OnInitComplete(e);

        }
        
        /// <summary>
        /// 获取当前用户
        /// </summary>
        public BaseUser GetUserInfo
        {
            get
            {
                BaseUser user = BaseUser.GetBaseUserHandler(this.Session);
                return user;
                
            }
        }


        
        private DbConfig _DB;
        /// <summary>
        /// 当前会话DB
        /// </summary>
        public DbConfig DB
        {
            get
            {

                if (_DB == null)
                {
                    if (GetDbConfigBySession == null)
                    {
                        //未指定，则使用默认
                        _DB = MainConfig.GetDbConfig();
                    }
                    else
                    {
                        _DB = GetDbConfigBySession(this.Session);
                    }
                    
                }
                return _DB;
            }
        }
        
        /// <summary>
        /// 检查是否登录超时
        /// </summary>
        /// <param name="context"></param>
        /// <returns></returns>
        public static bool IsOut(System.Web.HttpContext context)
        {
            return IsOut(context.Session);
        }
        /// <summary>
        /// 判断是否已经登出
        /// </summary>
        /// <param name="session"></param>
        /// <returns></returns>
        public static bool IsOut(HttpSessionState session)
        {
            return BaseUser.GetBaseUserHandler(session) == null;
        }
        /// <summary>
        /// 启用GZip的字符长度
        /// </summary>
        public static int GZipLength = 1024;
       
        /// <summary>
        /// 输出内容,输出内容，系统会自动判断并处理压缩
        /// </summary>
        /// <param name="page">输出的页</param>
        /// <param name="value">待输出的内容</param>
        public static void Write(System.Web.UI.Page page ,string value)
        {
            Write(page.Request, page.Response, value,System.Text.Encoding.UTF8);
            try
            {
                page.Response.Flush();
            }
            catch(Exception err)
            {
            }
        }
        /// <summary>
        /// 输出内容,输出内容，系统会自动判断并处理压缩
        /// </summary>
        /// <param name="page">输出的页</param>
        /// <param name="value">待输出的内容</param>
        /// <param name="encoding">编码格式</param>
        public static void Write(System.Web.UI.Page page, string value, System.Text.Encoding encoding)
        {
            Write(page.Request, page.Response, value, encoding);
            try
            {
                page.Response.Flush();
            }
            catch (Exception err)
            {
            }
        }
        /// <summary>
        /// 输出内容,输出内容，系统会自动判断并处理压缩
        /// </summary>
        /// <param name="page">输出的页</param>
        /// <param name="value">待输出的内容</param>
        public static void Write(System.Web.UI.Page page, byte[] value)
        {
            Write(page.Request, page.Response, value);
            try
            {
                page.Response.Flush();
            }
            catch(Exception er)
            {
            }
        }
        /// <summary>
        /// 输出内容,输出内容，系统会自动判断并处理压缩,默认采用UTF-8编码
        /// </summary>
        /// <param name="page">输出的页</param>
        /// <param name="value">待输出的内容</param>
        public static void Write(System.Web.HttpContext context, string value)
        {
            Write(context.Request, context.Response, value, System.Text.Encoding.UTF8);
        }
        /// <summary>
        /// 输出内容,输出内容，系统会自动判断并处理压缩
        /// </summary>
        /// <param name="context">输出的页</param>
        /// <param name="value">待输出的内容</param>
        public static void Write(System.Web.HttpContext context, byte[] value)
        {
            Write(context.Request, context.Response, value);
        }
        /// <summary>
        /// 输出内容,输出内容，系统会自动判断并处理压缩
        /// </summary>
        /// <param name="page">输出的页</param>
        /// <param name="value">待输出的内容</param>
        /// <param name="encoding">编码格式</param>
        public static void Write(System.Web.HttpContext context, string value, System.Text.Encoding encoding)
        {
            Write(context.Request, context.Response, value,encoding);
        }
        public static void WriteJson(HttpRequest request, HttpResponse response, string value)
        {
            byte[] bytes = System.Text.Encoding.UTF8.GetBytes(value);
            response.HeaderEncoding = System.Text.Encoding.UTF8;

            response.AppendHeader("Content-type", string.Format("application/json;charset={0}", System.Text.Encoding.UTF8.BodyName));
            Write(request, response, bytes);
        }
        public static void Write(HttpRequest request, HttpResponse response, string value)
        {
            Write(request, response, value, System.Text.Encoding.UTF8);
        }
       
        public static void Write(HttpRequest request, HttpResponse response, string value,System.Text.Encoding encoding)
        {
            byte[] bytes = encoding.GetBytes(value);
            response.HeaderEncoding = encoding;
            
            response.AppendHeader("Content-type", string.Format("text/html;charset={0}",encoding.BodyName));
            Write(request, response, bytes);
            
        }
        public static void Write(HttpRequest request, HttpResponse response, byte[] value)
        {
            int contentlength = value.Length;
            try
            {

                if (contentlength >= GZipLength)
                {
                    string acceptcode = request.Headers["Accept-Encoding"];
                    if (acceptcode != null && acceptcode.IndexOf("gzip") >= 0)
                    {
                        value = Zip.GZipCompress(value);
                        response.Headers["Content-Encoding"] = "gzip";
                    }
                }
                response.BinaryWrite(value);
            }
            catch
            {

            }
            finally
            {

                //if (response.IsClientConnected)
                //    response.Close();
            }
            
            
        }
    }

    public static class HttpContextHelp
    {
        public static WebActionData GetHandlerWebValue(this HttpContext httpContext)
        {
            WebActionData handlerWebValue = new WebActionData(httpContext.Request);
            return handlerWebValue;
        }
    }
}
