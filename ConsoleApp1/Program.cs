﻿using System;
using System.Net.Sockets;
using System.Net;
using System.Collections.Generic;
using System.Threading.Tasks;
using System.Text;
using System.Linq;

namespace ConsoleApp1
{
    class Program
    {

        public static List<TcpClient> list = new List<TcpClient>();
        //public static List<bool> conections = new List<bool>();
        public static Dictionary<TcpClient,bool> conections = new Dictionary<TcpClient, bool>();
        public static Dictionary<TcpClient, bool> readyforgame = new Dictionary<TcpClient, bool>();
        //public static List<bool> readyforgame = new List<bool>();
        public static TcpListener server = new TcpListener(IPAddress.Parse("192.168.0.102"), 8001);
        static void Main(string[] args)
        {
            server.Start(10);
            while (true)
            {
                TcpClient tcp = server.AcceptTcpClient();
                list.Add(tcp);
                Console.WriteLine($"Входящее подключение: {list[list.Count - 1].Client.RemoteEndPoint}");
                conections.Add(tcp,false);
                readyforgame.Add(tcp,true);
                int ind = list.Count - 1;
                Task t = new Task(()=>newclient(tcp,(byte)ind));
                t.Start();
            }
        }
         
        public static void newclient(TcpClient client,byte ind) {
            bool conected = false;
            bool flag = true;
            TcpClient user2 = null;
            NetworkStream stream = client.GetStream();
            try
            {
                while (flag)
                {
                    byte[] data = new byte[4096];
                    byte[] msg1 = new byte[2] { 255, ind };
                    Socket soc = client.Client;
                    bool t1 = soc.Poll(100, SelectMode.SelectRead);
                    bool t2 = (soc.Available == 0);
                    if (t1 && t2) {
                        flag = false;
                    }                       
                    if (stream.DataAvailable)
                    {
                        int count = stream.Read(data, 0, data.Length);
                        if (count != 0)
                        {
                            int code = data[0];
                            switch (code)
                            {
                                case 2://запрос на подключение
                                    {
                                        user2 = list[data[1]];
                                        byte[] msg = new byte[2] { 255, ind };
                                        conections[client] = true;
                                        Console.WriteLine(client.Client.RemoteEndPoint.ToString() + " send request for connection to " + user2.Client.RemoteEndPoint.ToString());
                                        user2.GetStream().Write(msg, 0, msg.Length);
                                        break;
                                    }
                                case 0://запрос на подключение
                                    {
                                        client.GetStream().Write(new byte[] { ind }, 0, 1);
                                        break;
                                    }
                                case 255://ответ на подключение
                                    {
                                        user2 = list[data[1]];
                                        byte[] msg = new byte[2] { 254, ind };
                                        Console.WriteLine(user2.Client.RemoteEndPoint.ToString() + " accept connection from " + client.Client.RemoteEndPoint.ToString());
                                        user2.GetStream().Write(msg, 0, msg.Length);
                                        client.GetStream().Write(msg, 0, msg.Length);
                                        conections[client] = true;
                                        conected = true;
                                        break;
                                    }
                                case 254://подтверждение подключения
                                    {
                                        Console.WriteLine(client.Client.RemoteEndPoint.ToString() + " in game ");
                                        conections[client] = true;
                                        conected = true;
                                        break;
                                    }
                                case 253:
                                    {
                                        if (data[1] != 255)
                                        {
                                            byte[] msg = new byte[2] { 253, ind };
                                            if (data[1] < list.Count)
                                            {
                                                Console.WriteLine(client.Client.RemoteEndPoint.ToString() + " broke connection with " + user2.Client.RemoteEndPoint.ToString());
                                                user2 = list[data[1]];
                                                user2.GetStream().Write(msg, 0, msg.Length);
                                            }
                                            //list.Remove(client);
                                            conections[client] = false;
                                            conections[list[data[1]]] = false;
                                            conected = false;
                                        }
                                        /*list.Remove(client);
                                        client.Close();*/
                                        break;
                                    }
                                case 1:
                                    {
                                        byte[] tmp = new byte[0];
                                        int i = 0;
                                        int send_ind = data[1];
                                        Console.WriteLine(client.Client.RemoteEndPoint.ToString() + " send LIST request ");
                                        foreach (TcpClient item in list)
                                        {
                                            if (conections[item]==false && readyforgame[item])
                                            {
                                                /*if (send_ind != i)
                                                {*/
                                                    string s = item.Client.RemoteEndPoint.ToString();
                                                    byte[] arr = new byte[21];
                                                    byte[] str = Encoding.UTF8.GetBytes(s);
                                                    str.CopyTo(arr, 0);
                                                    tmp = tmp.Concat(arr).ToArray();
                                                /*}
                                                i++;*/
                                            }
                                            i++;
                                        }
                                        byte[] msg = new byte[tmp.Length + 1];
                                        msg[0] = (byte)tmp.Length;
                                        Array.Copy(tmp, 0, msg, 1, tmp.Length);
                                        client.GetStream().Write(msg, 0, msg.Length);
                                        break;
                                    }
                                case 3:
                                    {
                                        Console.WriteLine(client.Client.RemoteEndPoint.ToString() + " send data to " + user2.Client.RemoteEndPoint.ToString());
                                        user2 = list[data[1]];
                                        user2.GetStream().Write(data, 0, data.Length);
                                        break;
                                    }
                                case 4:
                                    {
                                        Console.WriteLine(client.Client.RemoteEndPoint.ToString() + " in offline mode ");
                                        readyforgame[client] = false;
                                        break;
                                    }
                                case 5:
                                    {
                                        Console.WriteLine(client.Client.RemoteEndPoint.ToString() + " in online mode ");
                                        readyforgame[client] = true;
                                        break;
                                    }                                   
                                    //код 3 пересылка пакета
                            }
                        }
                    }
                }
                if (user2 != null)
                    try
                    {
                        user2.GetStream().Write(new byte[] { 253 }, 0, 1);
                    }
                    catch (Exception) { }
            }
            catch (System.ObjectDisposedException) {}
            Console.WriteLine(client.Client.RemoteEndPoint.ToString() + "dissconected");
            list.Remove(client);
            conections.Remove(client);
            readyforgame.Remove(client);
        }
    }
}
