using System;
using System.Collections.Generic;
using System.Linq;

namespace SimpleHttpServerLib
{
    public class SimpleHttpContext
    {
        public HttpPage Page;
        public Dictionary<string, string> Query = new Dictionary<string, string>();
        public Type CodeType;
        public bool Redirect;
        public string RedirectPath;

        public SimpleHttpRequest Request { get;  set; }
        public SimpleHttpResponse Response { get; set; } = new SimpleHttpResponse();
        public bool CloseConnection { get; set; }
        public string IP { get; set; }

        public void ParseQuery(string query)
        {
            var spl = query.Split(new char[] { '=','&' }).ToArray();
            if (spl.Count() == 1)
            {
                Query.Add(spl[0], null);
                return;
            }
            for (int i = 0; i < spl.Length; i += 2)
            {
                var nm = spl[i];
                var val = spl[i + 1];
                Query.Add(nm, val);
            }
        }
    }
}