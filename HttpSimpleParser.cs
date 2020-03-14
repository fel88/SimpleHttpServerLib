using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Xml;

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
