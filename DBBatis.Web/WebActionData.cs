using System.Collections;
using System.Web;
using System.Text;
using DBBatis.Action;
using DBBatis.JSON;

namespace DBBatis.Web
{
    /// <summary>
    /// 处理WebValue
    /// </summary>
    public class WebActionData : ActionData
    {
       public HttpRequest Request { get; private set; }
       internal bool jsonflag = false;
        public WebActionData(HttpRequest request)
        {
            if (request.ContentType.IndexOf("json") > 0)
            {
                jsonflag = true;
                this.Init(request.InputStream);
            }
            Request = request;
        }

        protected override string GetValue(string key)
        {
            return Request[key]; 
        }
        public override ICollection GetKeys()
        {
            if (jsonflag)
            {
                return base.GetKeys();
            }
            System.Collections.Specialized.StringCollection keys = new System.Collections.Specialized.StringCollection();
            foreach (string k in Request.QueryString.AllKeys)
            {
                keys.Add(k);
            }
            foreach(string k in Request.Form.AllKeys)
            {
                keys.Add(k);
            }

            return keys;
        }
    }

}
