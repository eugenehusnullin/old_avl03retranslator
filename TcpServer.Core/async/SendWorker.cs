using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Net.Sockets;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace TcpServer.Core.async
{
    class SendWorker
    {
        private ConcurrentQueue<KeyValuePair<byte[], SocketAsyncEventArgs>> queue;
        private Thread thread;
        private volatile bool running = true;
        private readonly int timeout = 200;
        private BaseConnector baseConnector;

        public SendWorker(ConcurrentQueue<KeyValuePair<byte[], SocketAsyncEventArgs>> queue, BaseConnector baseConnector)
        {
            this.queue = queue;
            this.baseConnector = baseConnector;
        }

        public void start()
        {
            thread = new Thread(() => process());
            thread.Start();
        }

        public void stop()
        {
            running = false;
            thread.Interrupt();
            thread.Join();
        }

        private void process()
        {
            KeyValuePair<byte[], SocketAsyncEventArgs> pair;
            
            while (running)
            {
                if (queue.TryDequeue(out pair))
                {
                    baseConnector.startSend(pair.Value, pair.Key);
                }
                else
                {
                    try
                    {
                        Thread.Sleep(timeout);
                    }
                    catch (ThreadInterruptedException)
                    {
                        return;
                    }
                }
            }
        }
    }
}
