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
    abstract class AbstractSendWorker
    {
        private ConcurrentQueue<KeyValuePair<byte[], SocketAsyncEventArgs>> queue;
        private Thread thread;
        private volatile bool running = true;
        private readonly int timeout = 200;

        public AbstractSendWorker(ConcurrentQueue<KeyValuePair<byte[], SocketAsyncEventArgs>> queue)
        {
            this.queue = queue;
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
                    send(pair);
                }
                else
                {
                    try
                    {
                        Thread.Sleep(timeout);
                        //lock (queue)
                        //{
                        //    Monitor.Wait(queue);
                        //}
                    }
                    catch (ThreadInterruptedException)
                    {
                        return;
                    }
                }
            }
        }

        public abstract void send(KeyValuePair<byte[], SocketAsyncEventArgs> pair);
    }
}
