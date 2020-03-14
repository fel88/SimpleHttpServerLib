using System;
using System.IO;

namespace SimpleHttpServerLib
{
    public class FileLogger : ILog
    {
        public FileLogger(string path)
        {
            LogFile = path;
        }

        public string LogFile = "log.txt";

        public void Log(string msg)
        {            
            lock (LogFile)
            {
                File.AppendAllText(LogFile, DateTime.Now.ToString() + ": " + msg + Environment.NewLine);
            }
        }
    }
}
