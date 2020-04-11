using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using System.Xml.Linq;

namespace SimpleHttpServerLib
{
    public abstract class UrlRouter
    {
        //byte data and mime-type
        public abstract Tuple<byte[], string> GetData(SimpleHttpRequest req);

        public virtual string GetPath(SimpleHttpRequest req)
        {
            var spl0 = req.Header.Split(new char[] { ' ', '?' }, StringSplitOptions.RemoveEmptyEntries).ToArray();
            var url = spl0[1];

            var spl3 = req.Header.Split(new char[] { '/', ' ', '?' }, StringSplitOptions.RemoveEmptyEntries)
                  .ToArray();

            SimpleHttpContext ctx = new SimpleHttpContext();
            if (req.Header.Contains("?"))
            {
                spl0[2] = Http​Utility.UrlDecode(spl0[2]);
                ctx.ParseQuery(spl0[2]);
            }



            var spl = url.Split(new char[] { '/' }, StringSplitOptions.RemoveEmptyEntries).ToArray();
            if (spl.Count() < 1) return null;
            var fr = CleanUrls.FirstOrDefault(z => z.Path == url);
            if (fr != null)
            {
                return fr.Target;
            }
            return null;
        }

        public virtual void LoadConfig(string path)
        {
            CleanUrls.Clear();
            var doc = XDocument.Load(path);
            foreach (var item in doc.Descendants("url"))
            {
                var cu = new CleanUrl();
                cu.Path = item.Attribute("path").Value;
                cu.Target = item.Attribute("target").Value;
                CleanUrls.Add(cu);
            }
        }

        public List<CleanUrl> CleanUrls = new List<CleanUrl>();
    }

    public class RouterMethodAttribute : Attribute
    {
        public string Path;        
    }

    public enum RouterAccessLevel
    {
        All, Users, Admin
    }
}
