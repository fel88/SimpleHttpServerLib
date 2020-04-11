using System.Linq;
using System.Text;

namespace SimpleHttpServerLib
{
    public abstract class RouterController
    {
        public abstract RouterAccessLevel Level { get; }
        public bool Process(SimpleHttpContext ctx, string url)
        {
            var req = ctx.Request;
            var mth = this.GetType().GetMethods();
            foreach (var item in mth)
            {

                var attr1 = item.GetCustomAttributes(typeof(RouterMethodAttribute), true);
                if (attr1 == null || attr1.Count() == 0) continue;
                var ma = attr1[0] as RouterMethodAttribute;
                if (ma.Path == url)
                {
                    item.Invoke(this, new[] { ctx });
                    if (ctx.Redirect)
                    {

                        var stream = req.Stream;
                        StringBuilder sb = new StringBuilder();

                        sb.Append("HTTP/1.1 303 See other\r\n");
                        foreach (var hitem in ctx.Response.Headers)
                        {
                            sb.Append($"{hitem.Key}: {hitem.Value}\r\n");
                        }
                        sb.Append("Location: " + ctx.RedirectPath + "\r\n\r\n");
                        //sb.Append("Refresh: 0; url=" + ctx.RedirectPath + "\r\n");                            


                        byte[] Buffer = Encoding.UTF8.GetBytes(sb.ToString());
                        stream.Write(Buffer, 0, Buffer.Length);
                        stream.Close();

                    }

                    return true;
                }
            }
            return false;
        }
    }
}
