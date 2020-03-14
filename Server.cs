﻿using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net;
using System.Net.Sockets;
using System.Text;
using System.Threading;

namespace SimpleHttpServerLib
{
    public class HttpServer
    {

        public static UrlRouter Router = null;
        public static int Port = 80;
        public static TcpListener listener;

        //public static ClientObject clientObject;


        public static string LogFile = "log.txt";
        public static void LogAppend(string str)
        {
            lock (LogFile)
            {
                File.AppendAllText(LogFile, DateTime.Now.ToString() + ": " + str);
            }
        }

        public static bool LogEnable = false;
        public static void Log(string str)
        {
            if (LogEnable)
            {
                LogAppend(str);
            }
        }


        public static List<HttpConnectInfo> Infos = new List<HttpConnectInfo>();
        public static int Connects = 0;
        public static int CommandsProcessed = 0;
        public static bool FilterIp = false;
        public static List<string> AllowedIps = new List<string>();

        public static Thread StartServer()
        {
            Thread tt = new Thread(() =>
            {
                //sockets();
                //return;
                try
                {
                    listener = new TcpListener(IPAddress.Any, Port);
                    Console.WriteLine("Http server was started, port: " + Port);
                    listener.Start();
                    Log("Connections waiting...");

                    while (true)
                    {
                        TcpClient client = listener.AcceptTcpClient();
                        var addr = (client.Client.RemoteEndPoint as IPEndPoint).Address;
                        var ip = addr.ToString();
                        if (!FilterIp || (AllowedIps.Contains(ip)))
                        {
                            HttpServer.Infos.Add(new HttpConnectInfo() { Ip = addr.ToString() });

                            while (HttpServer.Infos.Count > 100)
                            {
                                HttpServer.Infos.RemoveAt(0);
                            }

                            var clientObject = new HttpClientObject(client, HttpServer.Infos.Last());                            

                            Thread clientThread = new Thread(new ThreadStart(clientObject.Process));

                            clientThread.Start();
                            Connects++;
                        }
                        else
                        {
                            client.Close();
                        }
                    }
                }
                catch (Exception ex)
                {
                    Log(ex.Message);
                    Console.WriteLine(ex.Message);
                }
                finally
                {
                    if (listener != null)
                        listener.Stop();
                }
            });
            tt.IsBackground = true;
            tt.Start();
            return tt;

        }
    }

    public class HttpConnectInfo
    {

        public string Ip { get; set; }
        public DateTime ConnectTimestamp { get; set; }

    }
}
