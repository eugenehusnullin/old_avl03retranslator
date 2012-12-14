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
    public class SocketGroup
    {
        public volatile SocketAsyncEventArgs blockReceiveSAEA = null;
        public volatile SocketAsyncEventArgs blockSendSAEA = null;
        public volatile SocketAsyncEventArgs monReceiveSAEA = null;
        public volatile SocketAsyncEventArgs monSendSAEA = null;

        public AutoResetEvent waitWhileSendToMon = new AutoResetEvent(true);
        public AutoResetEvent waitWhileSendToBlock = new AutoResetEvent(true);

        //public void reset()
        //{
        //    blockSendSAEA = null;
        //    monReceiveSAEA = null;
        //    monSendSAEA = null;

        //    waitWhileSendToMon.Set();
        //    waitWhileSendToBlock.Set();
        //}
    }
}
