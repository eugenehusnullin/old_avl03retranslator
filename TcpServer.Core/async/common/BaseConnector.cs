using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Sockets;
using System.Text;
using System.Threading.Tasks;

namespace TcpServer.Core.async.common
{
    class BaseConnector
    {
        public delegate void MessageReceived(byte[] message, SocketAsyncEventArgs saea);
        public delegate void MessageSended(SocketAsyncEventArgs saea);
        public delegate void ConnectionFailed(SocketAsyncEventArgs saea);

        protected MessageReceived messageReceived;
        protected MessageSended messageSended;
        protected ConnectionFailed connectionFailed;

        protected SocketAsyncEventArgs createSaea(EventHandler<SocketAsyncEventArgs> eventHandler, int bufferSize)
        {
            var saea = new SocketAsyncEventArgs();
            saea.Completed += eventHandler;
            saea.SetBuffer(new byte[bufferSize], 0, bufferSize);
            saea.UserToken = new DataHoldingUserToken();
            return saea;
        }
    }
}
