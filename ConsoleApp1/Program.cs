using System;
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
        public static List<bool> conections = new List<bool>();
        public static TcpListener server = new TcpListener(IPAddress.Parse("192.168.0.104"), 8001);
        static void Main(string[] args)
        {
            server.Start(10);
            while (true)
            {
                TcpClient tcp = server.AcceptTcpClient();
                list.Add(tcp);
                conections.Add(false);
                int ind = list.Count - 1;
                Task t = new Task(()=>newclient(tcp,(byte)ind));
                t.Start();
            }
            Console.WriteLine($"Входящее подключение: {list[list.Count-1].Client.RemoteEndPoint}");
        }

        public static void newclient(TcpClient client,byte ind) {
            bool conected = false;
            TcpClient user2 = null;
            NetworkStream stream = client.GetStream();
            try
            {
                while (client.GetStream().CanRead && client.GetStream().CanWrite)
                {
                    byte[] data = new byte[4096];
                    byte[] msg1 = new byte[2] { 255, ind };
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
                                        conections[ind] = true;
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
                                        user2.GetStream().Write(msg, 0, msg.Length);
                                        client.GetStream().Write(msg, 0, msg.Length);
                                        conections[ind] = true;
                                        conected = true;
                                        break;
                                    }
                                case 254://подтверждение подключения
                                    {
                                        conections[ind] = true;
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
                                                user2 = list[data[1]];
                                                user2.GetStream().Write(msg, 0, msg.Length);
                                            }
                                            //list.Remove(client);
                                            conections[ind] = false;
                                            conections[data[1]] = false;
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
                                        foreach (TcpClient item in list)
                                        {
                                            if (conections[i]==false)
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
                                        }
                                        byte[] msg = new byte[tmp.Length + 1];
                                        msg[0] = (byte)tmp.Length;
                                        Array.Copy(tmp, 0, msg, 1, tmp.Length);
                                        client.GetStream().Write(msg, 0, msg.Length);
                                        break;
                                    }
                                case 3:
                                    {
                                        user2 = list[data[1]];
                                        user2.GetStream().Write(data, 0, data.Length);
                                        break;
                                    }
                                    //код 3 пересылка пакета
                            }
                        }
                    }
                }
                if (user2 != null)
                    user2.GetStream().Write(new byte[] { 253 }, 0, 1);
            }
            catch (System.ObjectDisposedException) {}
            list.RemoveAt(ind);
        }
    }
}
