using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net.Sockets;
using System.Text;

namespace SimpleHttpServerLib
{
    public class SimpleHttpRequest
    {
        public string Method = "GET";
                
        
        public bool IsComplete;
        public List<string> Raw = new List<string>();
        public string Header;
        public byte[] RawData;
        public long ContentLen;
        public string ContentType;
        public string Cookie { get; internal set; }
        public NetworkStream Stream;

        public void Parse()
        {
            //1. detect content type
            long len = -1;
            if (Raw.Any(z => z.StartsWith("Content-Length: ")))
            {
                var fr = Raw.First(z => z.StartsWith("Content-Length: "));
                len = long.Parse(fr.Split(new string[] { ":", " " }, StringSplitOptions.RemoveEmptyEntries)[1]);
            }
            ContentLen = len;
            if (Raw.Any(z => z.StartsWith("Content-Type: ")))
            {
                var fr = Raw.First(z => z.StartsWith("Content-Type: "));
                var sub = fr.Substring("Content-Type: ".Length);
                var spl1 = sub.Split(new char[] { ';', '=' }, StringSplitOptions.RemoveEmptyEntries).ToArray();
                var dataType = spl1[0];
                ContentType = dataType;
            }
        }

        public string ParseUrlencoded()
        {            
            Parse();
            
            List<byte> data1 = new List<byte>();
            while (true)
            {
                byte[] temp = new byte[2048];
                var remains = ContentLen - data1.Count;
                if (remains == 0) break;
                if (remains < 2048)
                {
                    temp = new byte[remains];
                    var cnt2 = Stream.Read(temp, 0, temp.Length);
                    data1.AddRange(temp.Take(cnt2).ToArray());
                    break;
                }
                var cnt1 = Stream.Read(temp, 0, temp.Length);
                if (cnt1 == 0) break;
                data1.AddRange(temp.Take(cnt1).ToArray());

            }

            var ms = new MemoryStream(data1.ToArray());
            //File.WriteAllBytes("raw.dat", data1.ToArray());
            RawData = data1.ToArray();

            var dat = Encoding.UTF8.GetString(ms.ToArray());
            return dat;

        }
        public MultipartItem[] ParseMultipart()
        {
            List<MultipartItem> ret = new List<MultipartItem>();

            var rdr = new StreamReader(Stream);
            long len = -1;
            if (Raw.Any(z => z.StartsWith("Content-Length: ")))
            {
                var fr = Raw.First(z => z.StartsWith("Content-Length: "));
                len = long.Parse(fr.Split(new string[] { ":", " " }, StringSplitOptions.RemoveEmptyEntries)[1]);
            }
            string boundary = "";
            if (Raw.Any(z => z.StartsWith("Content-Type: ")))
            {
                var fr = Raw.First(z => z.StartsWith("Content-Type: "));
                var sub = fr.Substring("Content-Type: ".Length);
                var spl1 = sub.Split(new char[] { ';', '=' }, StringSplitOptions.RemoveEmptyEntries).ToArray();
                var dataType = spl1[0];
                boundary = spl1[2];
            }

            //read raw data
            List<byte> data1 = new List<byte>();
            while (true)
            {
                byte[] temp = new byte[2048];
                var remains = len - data1.Count;
                if (remains == 0) break;
                if (remains < 2048)
                {
                    temp = new byte[remains];
                    var cnt2 = Stream.Read(temp, 0, temp.Length);
                    data1.AddRange(temp.Take(cnt2).ToArray());
                    break;
                }
                var cnt1 = Stream.Read(temp, 0, temp.Length);
                if (cnt1 == 0) break;
                data1.AddRange(temp.Take(cnt1).ToArray());

            }

            var ms = new MemoryStream(data1.ToArray());
            //File.WriteAllBytes("raw.dat", data1.ToArray());
            RawData = data1.ToArray();
            var strm = new StreamReader(ms, Encoding.UTF8);


            boundary = boundary.Insert(0, "--");
            var bts = Encoding.UTF8.GetBytes(boundary);

            List<byte[]> chunks = new List<byte[]>();
            List<byte> accum = new List<byte>();
            List<byte> accum2 = new List<byte>();
            foreach (var item in data1)
            {

                accum2.Add(item);
                if (accum2.Count > bts.Length)
                {
                    accum2.RemoveAt(0);
                }
                if (accum2.Count == bts.Length)
                {
                    bool good = true;
                    for (int i = 0; i < bts.Length; i++)
                    {
                        if (bts[i] != accum2[i])
                        {
                            good = false;
                            break;
                        }
                    }
                    if (good)
                    {
                        var arr = accum.Take(accum.Count - (bts.Length - 1)).ToArray();
                        if (arr.Length > 0)
                        {
                            chunks.Add(arr);
                        }
                        accum.Clear();
                        continue;
                    }
                }
                accum.Add(item);
            }

            List<string> raw = new List<string>();

            var ms2 = new MemoryStream(chunks[0]);
            var mrdr = new StreamReader(ms2);
            string fn = "";
            bool isFormData = false;
            while (true)
            {
                var ln = mrdr.ReadLine();
                if (ln == "" && raw.Any())
                {
                    string fileName = "";
                    if (raw.Any(z => z.StartsWith("Content-Disposition: ")))
                    {
                        var fr = raw.First(z => z.StartsWith("Content-Disposition: "));
                        var sub = fr.Substring("Content-Disposition: ".Length);
                        var spl1 = sub.Split(new char[] { ';' }, StringSplitOptions.RemoveEmptyEntries).ToArray();
                        if (sub.Contains("form-data"))
                        {
                            isFormData = true;
                            fn = spl1[1];
                        }
                        else
                        {
                            fileName = spl1[2];
                        }
                    }


                    fn = fileName.Split(new string[] { "filename", "=", "\"" }, StringSplitOptions.RemoveEmptyEntries)[1];
                    break;
                }
                if (ln != "")
                {
                    raw.Add(ln);
                }
            }


            //read file

            List<byte> file = new List<byte>();
            bool inside = false;
            byte[] marker = new byte[] { 0xd, 0xa, 0xd, 0xa };
            List<byte> accum3 = new List<byte>();
            for (int i = 0; i < chunks[0].Length; i++)
            {
                accum3.Add(chunks[0][i]);
                if (accum3.Count > 4)
                {
                    accum3.RemoveAt(0);
                }
                if (accum3.Count == marker.Length)
                {
                    bool good = true;
                    for (int j = 0; j < marker.Length; j++)
                    {

                        if (marker[j] != accum3[j])
                        { good = false; break; }
                    }
                    if (good)
                    {
                        inside = true;
                        continue;
                    }
                }
                if (inside)
                {
                    file.Add(chunks[0][i]);
                }
            }
            var file2 = file.Take(file.Count - 2).ToArray();
            ret.Add(new MultipartItem() { Name = fn, Data = file2 });
            //File.WriteAllBytes(fn, file2);


            return ret.ToArray();
        }

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
