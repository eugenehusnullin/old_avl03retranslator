using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Sockets;
using System.Text;
using System.Threading.Tasks;
using TcpServer.Core.async.common;

namespace TcpServer.Core.async.block
{
    public class ReceiveAllReadedHandler
    {
        public int handle(SocketAsyncEventArgs saea, DataHoldingUserToken userToken, out byte[] readyMessage)
        {
            readyMessage = new byte[userToken.prefixBytes.Length + saea.BytesTransferred - userToken.bytesDoneThisOp];
            Buffer.BlockCopy(userToken.prefixBytes, 0, readyMessage, 0, userToken.prefixBytes.Length);
            Buffer.BlockCopy(saea.Buffer, userToken.bytesDoneThisOp, readyMessage,
                userToken.prefixBytes.Length, saea.BytesTransferred - userToken.bytesDoneThisOp);

            userToken.bytesDoneThisOp = saea.BytesTransferred;
            userToken.messageBytes = readyMessage;
            userToken.messageLength = readyMessage.Length;

            return 0;
        }
    }
}
