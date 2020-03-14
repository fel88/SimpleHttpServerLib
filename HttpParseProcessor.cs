using System;
using System.CodeDom;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace SimpleHttpServerLib
{
    public abstract class HttpParseProcessor
    {
        public string StartTag;
        public string EndTag;
        public bool Inside;
        public string accum;
        public string accum2;
        public StringBuilder sbb;
        public SimpleHttpContext Context;
        public HttpParseProcessor(StringBuilder sb, SimpleHttpContext codeType)
        {
            sbb = sb;
            Context = codeType;
        }

        public void PushSymbol(char c)
        {
            accum += c;
            accum = accum.Remove(0, 1);
            if (!Inside)
            {
                if (accum.EndsWith(StartTag))
                {
                    Inside = true;
                    accum2 = "";
                    return;
                }
            }

            if (Inside)
            {
                if (accum.EndsWith(EndTag))
                {
                    
                    Inside = false;
                    Parse(accum2);
                }
                accum2 += c;
            }

        }
        public abstract void Parse(string input);
    }

}
