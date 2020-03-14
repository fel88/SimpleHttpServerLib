using System.Collections.Generic;

namespace SimpleHttpServerLib
{
    public class SimpleHttpRequest
    {
        public string Method = "GET";

        public bool IsComplete;
        public List<string> Raw = new List<string>();
        public string Header;

        public string Cookie { get; internal set; }

        public void PushString(string str)
        {
            Raw.Add(str);
            if (str == "\n\n" || str == "")
            {
                IsComplete = true;
                return;
            }
            if (Header == null)
            {
                if (str.StartsWith("POST"))
                {
                    Method = "POST";
                }
                Header = str;
            }
        }
    }
}
