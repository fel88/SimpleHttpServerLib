using System;
using System.Collections;
using System.Linq;
using System.Text;

namespace SimpleHttpServerLib
{
    public class JstlParseProcessor : HttpParseProcessor
    {

        public override void Parse(string input)
        {
            sbb = sbb.Remove(sbb.Length - (StartTag.Length), StartTag.Length);
            //get list
            //get var name
            //get all ${} entries
            var ind1 = input.IndexOf('>');
            var hdr = input.Substring(0, ind1);
            var spl = hdr.Split(new char[] { ' ', '=', '$', '{', '}' }, StringSplitOptions.RemoveEmptyEntries).ToArray();
            var collnm = spl[2];

            var fld = Context.CodeType.GetProperty(collnm);
            if (fld != null)
            {
                var val = fld.GetValue(Context.Page, new object[] { }) as IEnumerable;
                var en = val.GetEnumerator();
                while (en.MoveNext())
                {
                    var a = en.Current;
                    var tp1 = a.GetType();
                    bool inside = false;
                    string acc = "";
                    for (int i = ind1 + 1; i < input.Length - EndTag.Length; i++)
                    {
                        var c = input[i];
                        if (c == '$')
                        {
                            inside = true;
                            acc = "";
                            continue;
                        }

                        if (inside)
                        {
                            if (c == '}')
                            {
                                inside = false;
                                acc = acc.Replace("{", "");
                                var splpl = acc.Split(new char[] { '.' }, StringSplitOptions.RemoveEmptyEntries)
                                    .ToArray();

                                var fl2 = tp1.GetProperty(splpl[1]);
                                if (fl2 != null)
                                {
                                    var vall = fl2.GetValue(a, new object[] { });
                                    sbb.Append(vall.ToString());
                                }

                                continue;
                            }

                            acc += c;
                            continue;
                        }

                        sbb.Append(input[i]);
                    }
                }
            }
        }

        public JstlParseProcessor(StringBuilder sb, SimpleHttpContext ct) : base(sb, ct)
        {
            StartTag = "<c:forEach";
            EndTag = "</c:forEach>";
            accum = new string(' ', Math.Max(StartTag.Length, EndTag.Length));
        }
    }
}