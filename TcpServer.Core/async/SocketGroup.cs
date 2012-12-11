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
        public SocketAsyncEventArgs blockReceiveSAEA = null;
        public SocketAsyncEventArgs blockSendSAEA = null;
        public SocketAsyncEventArgs monReceiveSAEA = null;
        public SocketAsyncEventArgs monSendSAEA = null;

        //public ConcurrentQueue<byte[]> fromBlockData = new ConcurrentQueue<byte[]>();
        //public ConcurrentQueue<byte[]> fromMonData = new ConcurrentQueue<byte[]>();

        //public AutoResetEvent waitDataFromBlock = new AutoResetEvent(false);
        //public AutoResetEvent waitDataFromMon = new AutoResetEvent(false);

        public AutoResetEvent waitWhileSendToBlock = new AutoResetEvent(true);
        public AutoResetEvent waitWhileSendToMon = new AutoResetEvent(true);

        public void reset()
        {
            blockSendSAEA = null;
            monReceiveSAEA = null;
            monSendSAEA = null;

            waitWhileSendToBlock.Set();
            waitWhileSendToMon.Set();
        }
    }
}
