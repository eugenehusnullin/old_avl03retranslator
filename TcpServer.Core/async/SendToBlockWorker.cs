using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Net.Sockets;
using System.Text;
using System.Threading.Tasks;

namespace TcpServer.Core.async
{
    class SendToBlockWorker : AbstractSendWorker
    {
        public SendToBlockWorker(ConcurrentQueue<KeyValuePair<byte[], SocketAsyncEventArgs>> queue) : base(queue) { }

        public override void send(KeyValuePair<byte[], System.Net.Sockets.SocketAsyncEventArgs> pair)
        {
            ConnectionsAccepter.blockStartSend(pair.Value, pair.Key);
        }
    }
}
