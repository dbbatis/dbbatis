using DBBatis.Action;
using System.Threading.Tasks;
using System.Web;

namespace DBBatis.Web
{
    public class WebContextHandler : ContextHandlerBase
    {

        public override ActionData Data { get => _HandlerWebValue; set => _HandlerWebValue = value; }
        public override bool CanGZIP
        {
            get
            {
                string acceptcode = Context.Request.Headers["Accept-Encoding"];
                if (acceptcode != null && acceptcode.IndexOf("gzip") >= 0)
                {
                    return true;

                }
                return false;
            }
        }

        public override int UserID { 
            get
            {
                BaseUser tempv = BaseUser.GetBaseUserHandler( Context.Session);
                if (tempv != null) 
                    return tempv.ID;
                return 0;
            }
        }

        public override object Session => Context.Session;

       public HttpContext Context { get; private set; }

        public override System.Uri Url => Context.Request.Url;

        ActionData _HandlerWebValue;

        public WebContextHandler(HttpContext context)
        {
            Context = context;
            
            _HandlerWebValue = new WebActionData(Context.Request);
        }

        public override bool CheckValid()
        {

            return true;

        }
        public override Task Write(byte[] bytes, string contentType,bool gzip)
        {
            Context.Response.ContentType = contentType;
            if (gzip)
            {
                Context.Response.Headers["Content-Encoding"] = "gzip";
            }
            Context.Response.BinaryWrite(bytes);
            
            return Task.CompletedTask;
        }

        public override void SetSession(string key, object value)
        {
            ((HttpSessionStateBase)this.Session).Add(key, value);
        }

        public override object GetSession(string key)
        {
            return ((HttpSessionStateBase)this.Session)[key];
        }
    }
}
