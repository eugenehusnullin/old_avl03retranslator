using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Sockets;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace RepairFromLog
{
    class Sender
    {
        private Queue<string> queue = new Queue<string>();
        private object locker = new object();
        public string imei;
        private volatile bool stoped = false;
        private Thread thread;
        private string host;
        private int port;
        public int count = 0;

        public Sender(string imei, string host, int port)
        {
            this.imei = imei;
            this.host = host;
            this.port = port;
        }

        public void send(string line)
        {
            lock (locker)
            {
                queue.Enqueue(line);
            }
        }

        public void start()
        {
            stoped = false;
            thread = new Thread(() => work());
            thread.Start();
        }

        public void stop()
        {
            stoped = true;
            thread.Join();

            System.Console.Out.WriteLine(imei + " count: " + count + ".");
        }

        private void work()
        {
            var tcpClient = connect();
            NetworkStream stream = tcpClient.GetStream();

            while (!stoped || queue.Count != 0)
            {
                string line = null;
                lock (locker)
                {
                    if (queue.Count > 0)
                    {
                        line = queue.Dequeue();
                    }
                }

                if (line != null)
                {
                    bool sended = false;
                    bool reconect = false;
                    int trySendCount = 0;
                    while (!sended && trySendCount < 1000)
                    {
                        trySendCount++;
                        try
                        {
                            while (!tcpClient.Connected || reconect)
                            {
                                reconect = false;
                                Thread.Sleep(1000);
                                tcpClient = connect();
                                stream = tcpClient.GetStream();                                
                            }
                            var bytes = Encoding.ASCII.GetBytes(line);
                            stream.Write(bytes, 0, bytes.Length);
                            count++;
                            sended = true;
                        }
                        catch (Exception e)
                        {
                            Console.Out.WriteLine(e.ToString());
                            Console.Out.WriteLine(line);
                            Thread.Sleep(1000);
                            reconect = true;
                        }
                    }
                }

                if ((count % 1000) == 0)
                {
                    Console.Out.WriteLine(imei + " - " + count);
                }
                Thread.Sleep(1000);
            }
            stream.Close();
            tcpClient.Close();
        }
        

        private TcpClient connect()
        {
            var tcpClient = new TcpClient(host, port);
            tcpClient.NoDelay = true;
            return tcpClient;
        }
    }
}
