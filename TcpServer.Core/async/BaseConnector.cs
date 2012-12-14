using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Sockets;
using System.Text;
using System.Threading.Tasks;

namespace TcpServer.Core.async
{
    abstract class BaseConnector
    {
        protected SocketAsyncEventArgs createSaea(EventHandler<SocketAsyncEventArgs> eventHandler, int bufferSize)
        {
            var saea = new SocketAsyncEventArgs();
            saea.Completed += eventHandler;
            saea.SetBuffer(new byte[bufferSize], 0, bufferSize);
            saea.UserToken = new DataHoldingUserToken();
            return saea;
        }

        abstract public void startSend(SocketAsyncEventArgs saea, byte[] bytes);
    }
}
