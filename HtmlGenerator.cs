using System;
using System.Collections.Generic;
using System.IO;
using System.Reflection;
using System.Text;

namespace SimpleHttpServerLib
{
    public class HtmlGenerator
    {

        public static string DirPath = "";
        public static Assembly Assembly { get; set; }
        public static Type DefaultCodeType { get; set; }
        public static Dictionary<string, Type> CodeTypes = new Dictionary<string, Type>();

        public static bool IsLinux
        {
            get
            {
                int p = (int)Environment.OSVersion.Platform;
                return (p == 4) || (p == 6) || (p == 128);
            }
        }

        public static string GetAbsolutePath(string path)
        {
            if (IsLinux)
            {
                return DirPath + path;
            }

            var res = DirPath + path.Replace("/", "\\");

            return res;
            //return Path.Combine(DirPath, path.Replace("/","\\"));
        }

        public static string Get(string path)
        {
            if (string.IsNullOrEmpty(path))
            {
                return File.ReadAllText(Path.Combine(DirPath, "Index.htm"), Encoding.UTF8);
            }
            //return File.ReadAllText(Path.Combine(DirPath, path), Encoding.UTF8);
            return File.ReadAllText(path, Encoding.UTF8);

        }
        public static string Header()
        {
            return File.ReadAllText("Response.htm");

        }
    }

}
