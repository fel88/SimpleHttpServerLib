using System.Collections.Generic;
using System.Linq;

namespace SimpleHttpServerLib
{
    public class SessionInfo
    {        
        public static List<SessionInfo> Sessions = new List<SessionInfo>();
        public Dictionary<string, object> Storage = new Dictionary<string, object>();
        public static SessionInfo AddSession(string cookie)
        {
            lock (Sessions)
            {
                var s = new SessionInfo() { Cookie = cookie };
                Sessions.Add(s);
                return s;
            }
        }
        public static SessionInfo Get(string cookie)
        {
            lock (Sessions)
            {
                return Sessions.FirstOrDefault(z => z.Cookie == cookie);
            }
        }

        public static void DeleteSession(SessionInfo sess)
        {
            lock (Sessions)
            {
                Sessions.Remove(sess);
            }
        }

        public string Cookie;
    }
}
