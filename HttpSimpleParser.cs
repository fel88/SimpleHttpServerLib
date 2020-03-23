using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;

namespace SimpleHttpServerLib
{
    public class HttpSimpleParser : HttpParseProcessor
    {

        public HttpSimpleParser(StringBuilder sb, SimpleHttpContext ct) : base(sb, ct)
        {
            StartTag = "<%";
            EndTag = "%>";
            accum = new string(' ', Math.Max(StartTag.Length, EndTag.Length));
        }


        public override void Parse(string input)
        {
            sbb = sbb.Remove(sbb.Length - (StartTag.Length), StartTag.Length);
            var asm = Context.CodeType;
            accum2 = accum2.Replace("%", "").Trim();

            if (accum2.Contains("template"))
            {
                var templ = File.ReadAllText(accum2);

            }
            else
            {
                foreach (var info in asm.GetMethods())
                {
                    if (info.Name == accum2)
                    {
                        var prs = info.GetParameters();
                        if (prs.Any() && prs.Last().ParameterType == typeof(SimpleHttpContext))
                        {
                            var res = info.Invoke(Context.Page, new object[] { Context });
                            sbb.Append(res);
                        }
                        else
                        {
                            var res = info.Invoke(Context.Page, new object[] { });
                            sbb.Append(res);
                        }
                    }
                }
            }
        }
    }

    public class TemplateParser : HttpParseProcessor
    {

        public TemplateParser(StringBuilder sb, SimpleHttpContext ct) : base(sb, ct)
        {
            StartTag = "<%%";
            EndTag = "%%>";
            accum = new string(' ', Math.Max(StartTag.Length, EndTag.Length));
        }

        public static string Build(string html, SimpleHttpContext ctx)
        {
            List<HttpParseProcessor> procs = new List<HttpParseProcessor>();
            StringBuilder output = new StringBuilder();


            procs.Add(new TemplateParser(output, ctx));

            string accum = "";
            for (int i = 0; i < html.Length; i++)
            {
                accum += html[i];
                while (accum.Length > 10)
                {
                    accum = accum.Remove(0, 1);
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
        public override void Parse(string input)
        {
            sbb = sbb.Remove(sbb.Length - (StartTag.Length), StartTag.Length);
            
            accum2 = accum2.Replace("%", "").Trim();


            var p = HtmlGenerator.GetAbsolutePath(accum2);
            
            var templ = File.ReadAllText(p);
            
            sbb.Append(Build(templ, Context));


        }
    }
}
