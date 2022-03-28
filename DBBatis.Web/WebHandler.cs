using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Web;

namespace DBBatis.Web
{
    public class WebHandler : IHttpHandler, System.Web.SessionState.IReadOnlySessionState
    {
        public bool IsReusable => true;

        public void ProcessRequest(HttpContext context)
        {
            WebContextHandler handler = new WebContextHandler(context);

            handler.Precess();
        }

    }
}
