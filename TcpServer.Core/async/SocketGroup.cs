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
    class SocketGroup
    {
        public volatile SocketAsyncEventArgs blockReceiveSAEA = null;
        public volatile SocketAsyncEventArgs blockSendSAEA = null;
        public volatile SocketAsyncEventArgs monReceiveSAEA = null;
        public volatile SocketAsyncEventArgs monSendSAEA = null;

        public AutoResetEvent waitWhileSendToBlock = new AutoResetEvent(true);
        public AutoResetEvent waitWhileSendToMon = new AutoResetEvent(true);

        private volatile int usedSaeas = 0;
        private object syncObject = new object();


        public AutoResetEvent waitUsed = new AutoResetEvent(true);

        public void incrementUsed()
        {
            lock (syncObject)
            {
                waitUsed.Reset();
                usedSaeas++;
            }
        }

        public void decrementUsed()
        {
            lock (syncObject)
            {
                usedSaeas--;
                if (usedSaeas == 0)
                {
                    waitUsed.Set();
                }
            }
        }

        //public void reset()
        //{
        //    blockSendSAEA = null;
        //    monReceiveSAEA = null;
        //    monSendSAEA = null;

        //    waitWhileSendToBlock.Set();
        //    waitWhileSendToMon.Set();

        //    usedSaeas = 0;
        //    waitUsed.Set();
        //}
    }
}
