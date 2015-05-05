using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Net.Sockets;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace TcpServer.Core.async.common
{
    public class SocketGroup
    {
        public SocketAsyncEventArgs blockReceiveSAEA = null;
        public SocketAsyncEventArgs blockSendSAEA = null;
        public SocketAsyncEventArgs monReceiveSAEA = null;
        public SocketAsyncEventArgs monSendSAEA = null;
        public SocketAsyncEventArgs mon2ReceiveSAEA = null;
        public SocketAsyncEventArgs mon2SendSAEA = null;
        public string IMEI = null;
        public string LastCmd = null;

    }
}
