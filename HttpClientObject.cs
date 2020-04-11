using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net.Sockets;
using System.Text;
using System.Threading;
using System.Net;
using System.Web;

namespace SimpleHttpServerLib
{
    public class HttpClientObject
    {
        public HttpClientObject(TcpClient client, HttpConnectInfo info)
        {
            Client = client;
            Info = info;
        }

        TcpClient Client;
        Thread th;
        public HttpConnectInfo Info;

        public ILog Logger;

        public string CookieId;

        SimpleHttpRequest currentRequest = null;

        public void Process()
        {
            th = new Thread(() =>
            {
                NetworkStream _stream = null;
                try
                {
                    var info = Info;
                    info.ConnectTimestamp = DateTime.Now;

                    var stream = Client.GetStream();
                    _stream = stream;
                    var rdr = new StreamReader(stream);

                    //byte[] data = new byte[2048];

                    /*StringBuilder sb = new StringBuilder();
                    while (true)
                    {
                        var cnt = stream.Read(data, 0, data.Length);
                        sb.Append(Encoding.UTF8.GetString(data.Take(cnt).ToArray()));
                        if (cnt == 0) break;
                    } */


                    while (true)
                    {
                        //var str = rdr.ReadLine();
                        StringBuilder strb = new StringBuilder();
                        List<byte> dd = new List<byte>();
                        byte[] data = new byte[2];
                        while (true)
                        {
                            var bb1 = stream.ReadByte();
                            if (bb1 == -1) break;
                            var bb = (byte)bb1;
                            data[0] = data[1];
                            data[1] = bb;
                            dd.Add(bb);
                            if (data[0] == 0xd && data[1] == 0xa)
                            {
                                dd.RemoveAt(dd.Count - 1);
                                dd.RemoveAt(dd.Count - 1);
                                break;
                            }
                        }

                        var str = Encoding.UTF8.GetString(dd.ToArray());

                        Logger?.Log(str);
                        if (str == null) break;
                        if (currentRequest == null)
                        {
                            currentRequest = new SimpleHttpRequest();
                        }

                        currentRequest.PushString(str);

                        if (currentRequest.IsComplete)
                        {
                            currentRequest.Stream = stream;

                            var ctx = ProcessRequest(currentRequest, Client);

                            currentRequest = null;
                            if (SingletonCall || (ctx != null && ctx.CloseConnection))
                            {
                                stream.Close();
                                break;
                            }
                        }
                    }
                }
                catch (Exception ex)
                {
                    Logger?.Log(ex.Message);
                    HttpServer.Log("[" + Info.Ip + "] disconnected: " + ex.Message);
                    Console.WriteLine("[" + Info.Ip + "] disconnected: " + ex.Message);
                    // ignored
                }
            });
            th.IsBackground = true;
            th.Start();
        }


        public class MimeInfo
        {
            public string Extension;
            public string Mime;
        }

        public static List<MimeInfo> Mimes = new List<MimeInfo>();

        static HttpClientObject()
        {
            Mimes.Add(new MimeInfo() { Extension = "css", Mime = "text/css" });
            Mimes.Add(new MimeInfo() { Extension = "js", Mime = "application/javascript" });
            Mimes.Add(new MimeInfo() { Extension = "wmv", Mime = "video/x-ms-wmv" });
            Mimes.Add(new MimeInfo() { Extension = "mp4", Mime = "video/mp4" });
            Mimes.Add(new MimeInfo() { Extension = "png", Mime = "image/png" });
            Mimes.Add(new MimeInfo() { Extension = "ico", Mime = "image/x-icon" });
            Mimes.Add(new MimeInfo() { Extension = "jpg", Mime = "image/jpeg" });
            Mimes.Add(new MimeInfo() { Extension = "jpeg", Mime = "image/jpeg" });
            Mimes.Add(new MimeInfo() { Extension = "svg", Mime = "image/svg+xml" });

        }

        private SimpleHttpContext ProcessRequest(SimpleHttpRequest currentRequest, TcpClient client)
        {
            var stream = currentRequest.Stream;
            
            var addr = (client.Client.RemoteEndPoint as IPEndPoint).Address;
            var ip = addr.ToString();
            
            if (currentRequest.Method == "POST")
            {
                var cc = currentRequest.Raw.FirstOrDefault(z => z.StartsWith("Cookie"));

                if (cc != null)
                {
                    var spl2 = cc.Split(new char[] { ':', ' ' }, StringSplitOptions.RemoveEmptyEntries).ToArray();
                    var cookieId = spl2[1];
                    currentRequest.Cookie = cookieId;
                }

                /*var g = Guid.NewGuid().ToString().Replace("-", "");
                CookieId = g;
                string Str = "HTTP/1.1 302 Found\nSet-Cookie: " + g + "\n\n";
                byte[] Buffer = Encoding.Default.GetBytes(Str);
                stream.Write(Buffer, 0, Buffer.Length);*/
                if (HttpServer.Router != null)
                {
                    try
                    {
                        var spl22 = currentRequest.Header.Split(new char[] { ' ', '?' }, StringSplitOptions.RemoveEmptyEntries)
                       .ToArray();
                        var tpl = HttpServer.Router.GetData(currentRequest);

                        if (tpl != null)
                        {
                            var bb = tpl.Item1;
                            var mime = tpl.Item2;
                            string Str = $"HTTP/1.1 200 OK\nContent-type: {mime}\nContent-Length:" +
                            bb.Length.ToString() + "\n\n";
                            byte[] Buffer = Encoding.UTF8.GetBytes(Str);

                            stream.Write(Buffer, 0, Buffer.Length);
                            stream.Write(bb, 0, bb.Length);
                            return null;
                        }
                    }
                    catch (Exception ex)
                    {
                        string err = "<p>" + ex.Message + "</p><p>" + ex.StackTrace.ToString() + "</p>";
                        string Str = "HTTP/1.1 200 OK\nContent-type: text/html\nContent-Length:" +
                                     err.Length.ToString() + "\n\n" + err;

                        byte[] Buffer = Encoding.UTF8.GetBytes(Str);
                        stream.Write(Buffer, 0, Buffer.Length);
                        return null;
                    }
                }

                var spl = currentRequest.Header.Split(new char[] { '/', ' ', '?' }, StringSplitOptions.RemoveEmptyEntries)
                 .ToArray();
                var splh = currentRequest.Header.Split(new string[] { "POST", "GET", "HTTP", " ", "?" }, StringSplitOptions.RemoveEmptyEntries)
                 .ToArray();


                string path = spl[1];
                path = splh[0];

                Console.WriteLine($"[{ip}] request: " + path);
                HttpServer.Log($"[{ip}] request: " + path);

                var ctx = new SimpleHttpContext();
            
                if (HtmlGenerator.CodeTypes.ContainsKey(path))
                {
                    ctx.CodeType = HtmlGenerator.CodeTypes[path];
                }
                else
                {
                    ctx.CodeType = HtmlGenerator.DefaultCodeType;
                }
                if (currentRequest.Header.Contains("?"))
                {
                    var query = spl[2];
                    query = Http​Utility.UrlDecode(query);
                    ctx.ParseQuery(query);
                }
                ctx.Page = Activator.CreateInstance(ctx.CodeType) as HttpPage;
                ctx.Request = currentRequest;
                ctx.IP = ip;
                ctx.Page.OnPageLoad(ctx);


            }
            else if (currentRequest.Method == "GET")
            {

                var spl = currentRequest.Header.Split(new char[] { '/', ' ', '?' }, StringSplitOptions.RemoveEmptyEntries)
                   .ToArray();
                var splh = currentRequest.Header.Split(new string[] { "GET", "HTTP", " ", "?" }, StringSplitOptions.RemoveEmptyEntries)
                 .ToArray();

                string Html = "<html><body><h1>It works!</h1></body></html>";
                string path = spl[1];
                path = splh[0];

                if (spl[1] == "HTTP")
                {
                    path = "";
                }

                if (string.IsNullOrEmpty(path))
                {
                    path = "Index.htm";
                }

                Console.WriteLine($"[{ip}] request: " + path);
                HttpServer.Log($"[{ip}] request: " + path);

                Logger?.Log("domain dir: " + System.AppDomain.CurrentDomain.BaseDirectory);
                Logger?.Log("path: " + path);
                Logger?.Log("abs path:" + HtmlGenerator.GetAbsolutePath(path));
                Logger?.Log("exist: " + File.Exists(HtmlGenerator.GetAbsolutePath(path)));

                //check router (clean url)

                bool handled = false;
                var cc = currentRequest.Raw.FirstOrDefault(z => z.StartsWith("Cookie"));
                if (cc != null)
                {
                    var spl2 = cc.Split(new char[] { ':', ' ' }, StringSplitOptions.RemoveEmptyEntries).ToArray();
                    var cookieId = spl2[1];
                    currentRequest.Cookie = cookieId;
                }
                if (HttpServer.Router != null)
                {
                    try
                    {
                        //var spl22 = currentRequest.Header.Split(new char[] { ' ', '?' }, StringSplitOptions.RemoveEmptyEntries)
                        //.ToArray();
                        var pth = HttpServer.Router.GetPath(currentRequest);
                        if (pth != null)
                        {
                            path = pth;
                        }
                        else
                        {
                            var tpl = HttpServer.Router.GetData(currentRequest);

                            if (tpl != null)
                            {
                                var bb = tpl.Item1;
                                var mime = tpl.Item2;
                                string Str = $"HTTP/1.1 200 OK\nContent-type: {mime}; charset=utf-8\nContent-Length:" +
                                bb.Length.ToString() + "\n\n";
                                byte[] Buffer = Encoding.UTF8.GetBytes(Str);

                                stream.Write(Buffer, 0, Buffer.Length);
                                stream.Write(bb, 0, bb.Length);
                                return null;
                            }
                        }
                    }
                    catch (Exception ex)
                    {
                        string err = "<p>" + ex.Message + "</p><p>" + ex.StackTrace.ToString() + "</p>";
                        string Str = "HTTP/1.1 200 OK\nContent-type: text/html; charset=utf-8\nContent-Length:" +
                                     err.Length.ToString() + "\n\n" + err;

                        byte[] Buffer = Encoding.UTF8.GetBytes(Str);
                        stream.Write(Buffer, 0, Buffer.Length);
                        return null;
                    }

                }

                foreach (var item in Mimes)
                {
                    var p1 = (HtmlGenerator.GetAbsolutePath(path));
                    if (path.EndsWith(item.Extension) && File.Exists(p1))
                    {
                        var bb = File.ReadAllBytes(HtmlGenerator.GetAbsolutePath(path));

                        string Str = $"HTTP/1.1 200 OK\nContent-type: {item.Mime}; charset=utf-8\nContent-Length:" +
                                     bb.Length.ToString() + "\n\n";
                        byte[] Buffer = Encoding.UTF8.GetBytes(Str);

                        stream.Write(Buffer, 0, Buffer.Length);
                        stream.Write(bb, 0, bb.Length);
                        return null;
                    }
                }


                if (path.EndsWith("htm") && File.Exists(HtmlGenerator.GetAbsolutePath(path)))
                {
                    Html = HtmlGenerator.Get(HtmlGenerator.GetAbsolutePath(path));

                    SimpleHttpContext ctx = new SimpleHttpContext();

                    if (currentRequest.Header.Contains("?"))
                    {
                        var query = spl[2];
                        query = Http​Utility.UrlDecode(query);
                        ctx.ParseQuery(query);
                    }


                    //var cc = currentRequest.Raw.FirstOrDefault(z => z.StartsWith("Cookie"));
                    //if (cc != null)
                    //{
                    //    var spl2 = cc.Split(new char[] { ':', ' ' }, StringSplitOptions.RemoveEmptyEntries).ToArray();
                    //    var cookieId = spl2[1];
                    //    currentRequest.Cookie = cookieId;
                    //}

                    if (HtmlGenerator.CodeTypes.ContainsKey(path))
                    {
                        ctx.CodeType = HtmlGenerator.CodeTypes[path];
                    }
                    else
                    {
                        ctx.CodeType = HtmlGenerator.DefaultCodeType;
                    }
                    ctx.Page = Activator.CreateInstance(ctx.CodeType) as HttpPage;

                    try
                    {
                        ctx.Request = currentRequest;
                        ctx.IP = ip;

                        Html = DynamicUpdate(Html, ctx);
                        if (ctx.Redirect)
                        {
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
                        else
                        {

                            StringBuilder sb = new StringBuilder();
                            sb.Append("HTTP/1.1 200 OK\n");
                            foreach (var hitem in ctx.Response.Headers)
                            {
                                sb.Append($"{hitem.Key}: {hitem.Value}\n");
                            }

                            var len = Encoding.UTF8.GetBytes(Html).Length;

                            sb.Append("Content-type: text/html; charset=utf-8\nContent-Length:" +
                                         //Html.Length.ToString()
                                         len.ToString()
                                         + "\n\n" + Html);

                            var Str = sb.ToString();

                            byte[] Buffer = Encoding.UTF8.GetBytes(Str);

                            stream.Write(Buffer, 0, Buffer.Length);
                        }
                        return ctx;

                    }
                    catch (Exception ex)
                    {
                        string err = "<p>" + ex.Message + "</p><p>" + ex.StackTrace.ToString() + "</p>";
                        string Str = "HTTP/1.1 200 OK\nContent-type: text/html; charset=utf-8\nContent-Length:" +
                                     err.Length.ToString() + "\n\n" + err;

                        byte[] Buffer = Encoding.UTF8.GetBytes(Str);
                        stream.Write(Buffer, 0, Buffer.Length);
                    }
                }

                if (!handled)
                {
                    if (AllowAccessToUnhandledFiles)
                    {
                        try
                        {
                            if (File.Exists(HtmlGenerator.GetAbsolutePath(path)))
                            {
                                var bb = File.ReadAllBytes(HtmlGenerator.GetAbsolutePath(path));
                                HttpStuff.SendResponse(currentRequest.Stream, bb);
                            }
                        }
                        catch (Exception ex)
                        {

                        }
                    }
                    else
                    {
                        Console.WriteLine($"[{ip}] {path}: 404 Not Found");
                        StringBuilder sb = new StringBuilder();

                        string err = "<p>404. Not Found</p>";
                        string Str = "HTTP/1.1 404 Not Found\nContent-type: text/html; charset=utf-8\nContent-Length:" +
                                     err.Length.ToString() + "\n\n" + err;

                        byte[] Buffer = Encoding.UTF8.GetBytes(Str);
                        stream.Write(Buffer, 0, Buffer.Length);




                    }

                }


            }
            return null;
        }


        public static bool AllowAccessToUnhandledFiles = false;

        public static bool SingletonCall = false;


        private string DynamicUpdate(string html, SimpleHttpContext ctx)
        {
            html = TemplateParser.Build(html, ctx);
            //search all <%

            bool inside = false;
            StringBuilder output = new StringBuilder();
            string insd = "";

            ctx.Page.Context = ctx;
            ctx.Page.OnPageLoad(ctx);
            if (ctx.Response.OverrideResponse)
            {
                return ctx.Response.OverrideResponseOutput;
            }
            if (ctx.Redirect) return null;
            /*var plm = ctx.CodeType.GetMethod("OnPageLoad");
            if (plm != null)
            {
                plm.Invoke(null, new object[] { ctx });
            }*/
            List<HttpParseProcessor> procs = new List<HttpParseProcessor>();


            procs.Add(new HttpSimpleParser(output, ctx));
            procs.Add(new JstlParseProcessor(output, ctx));

            string accum = "";
            for (int i = 0; i < html.Length; i++)
            {
                accum += html[i];
                while (accum.Length > 10)
                {
                    accum = accum.Remove(0, 1);
                }

                if (accum.Contains("<c:"))
                {

                }

                if (procs.Any(z => z.Inside))
                {
                    var fr = procs.First(z => z.Inside);
                    fr.PushSymbol(html[i]);
                    continue;
                }
                foreach (var proc in procs)
                {
                    proc.PushSymbol(html[i]);
                    if (proc.Inside) break;
                }

                output.Append(html[i]);
            }

            return output.ToString();
        }

        internal void Abort()
        {
            if (th != null)
            {
                th.Abort();
            }
        }
    }
}
