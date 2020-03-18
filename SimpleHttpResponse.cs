using System.Collections.Generic;

namespace SimpleHttpServerLib
{
    public class SimpleHttpResponse
    {
        public Dictionary<string, string> Headers = new Dictionary<string, string>();
        public bool OverrideResponse;
        public string OverrideResponseOutput;
    }
}