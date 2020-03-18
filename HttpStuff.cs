using System.Net.Sockets;
using System.Text;
using System.Web;

namespace SimpleHttpServerLib
{
    public static class HttpStuff
    {
        public static void SendResponse(NetworkStream stream, string data)
        {
            string Str = "HTTP/1.1 200 OK\nContent-type: text/html; charset=utf-8\nContent-Length:" +
                             data.Length.ToString() + "\n\n" + data;

            byte[] Buffer = Encoding.UTF8.GetBytes(Str);
            stream.Write(Buffer, 0, Buffer.Length);
        }

        public static void SendResponsePlain(NetworkStream stream, string data)
        {
            string Str = "HTTP/1.1 200 OK\nContent-type: text/plain; charset=utf-8\nContent-Length:" +
                             data.Length.ToString() + "\n\n" + data;

            byte[] Buffer = Encoding.UTF8.GetBytes(Str);
            stream.Write(Buffer, 0, Buffer.Length);
        }
        public static void SendResponse(NetworkStream stream, byte[] data)
        {
            string Str = "HTTP/1.1 200 OK\nContent-type: text/html; charset=utf-8\nContent-Length:" +
                             data.Length.ToString() + "\n\n";

            byte[] Buffer = Encoding.UTF8.GetBytes(Str);
            stream.Write(Buffer, 0, Buffer.Length);
            stream.Write(data, 0, data.Length);
            stream.Flush();
        }
        public static void SendFileResponse(NetworkStream stream, string fileName, byte[] data)
        {
            string Str = "HTTP/1.1 200 OK\nContent-type: application/octet-stream; charset=utf-8\n";
            var fn = HttpUtility.UrlEncode(fileName);
            //inline, attachment
            Str += "Content-Disposition: attachment; filename=\"" + fn + "\"\n";
            //Str += "Content-Disposition: attachment; filename*=UTF-8''\"" + fileName + "\"\n";            



            Str += "Content-Length:" +
                         data.Length.ToString() + "\n\n";

            byte[] Buffer = Encoding.UTF8.GetBytes(Str);
            stream.Write(Buffer, 0, Buffer.Length);
            stream.Write(data, 0, data.Length);
            stream.Flush();
        }
    }
}
