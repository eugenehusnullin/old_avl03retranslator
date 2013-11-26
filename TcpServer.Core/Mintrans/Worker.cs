using log4net;
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace TcpServer.Core.Mintrans
{
    public class Worker
    {
        private SoapSink soapSink;
        private IUnifiedProtocolSettings settings;
        private Thread thread;
        private ILog log;
        private ConcurrentQueue<byte[]> messages;
        private int sleepTimeout = 5000;
        private volatile bool stoped = false;

        public Worker(IUnifiedProtocolSettings settings, ConcurrentQueue<byte[]> messages, ILog log)
        {
            this.settings = settings;
            this.soapSink = new SoapSink(settings);
            this.messages = messages;
            this.log = log;
        }

        public void start()
        {
            stoped = false;
            thread = new Thread(() => process());
            thread.Start();
        }

        public void stop()
        {
            stoped = true;
            thread.Interrupt();
            thread.Join();
        }

        public void process()
        {
            while (!stoped)
            {
                byte[] message;
                if (messages.TryDequeue(out message))
                {
                    try
                    {
                        soapSink.PostSoapMessage(message);
                        log.Info("Olympstroi sended success!");
                    }
                    catch(ApplicationException ae)
                    {
                        log.Error("Worker.process: " + this.settings.Url + ": " + ae.ToString());
                    }
                }
                else
                {
                    try
                    {
                        Thread.Sleep(sleepTimeout);
                    }
                    catch
                    {
                    }
                }
            }
        }
    }
}
