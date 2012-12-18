using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Sockets;
using System.Text;
using System.Threading.Tasks;
using TcpServer.Core.async.common;

namespace TcpServer.Core.async.block
{
    class ReceiveResponseHandler
    {
        public int handleResponse(SocketAsyncEventArgs saea, DataHoldingUserToken userToken, out byte[] readyMessage)
        {
            int cnt = 0;

            while (saea.BytesTransferred > (userToken.bytesDoneCountThisOp + cnt))
            {
                if (saea.Buffer[userToken.bytesDoneCountThisOp + cnt] == 0x23)
                {
                    cnt++;
                    break;
                }
                cnt++;
            }

            byte[] message = new byte[cnt + userToken.messageBytesDoneCount];

            if (userToken.messageBytesDoneCount != 0)
            {
                Buffer.BlockCopy(userToken.messageBytes, 0, message, 0, userToken.messageBytesDoneCount);
            }
            Buffer.BlockCopy(saea.Buffer, userToken.bytesDoneCountThisOp, message, userToken.messageBytesDoneCount, cnt);
            userToken.messageBytes = message;

            userToken.messageBytesDoneCount += cnt;
            userToken.bytesDoneCountThisOp += cnt;

            readyMessage = null;
            if (userToken.messageBytes[userToken.messageBytes.Length - 1] == 0x23)
            {
                readyMessage = new byte[userToken.prefixBytes.Length + userToken.messageBytes.Length];
                Buffer.BlockCopy(userToken.prefixBytes, 0, readyMessage, 0, userToken.prefixBytes.Length);
                Buffer.BlockCopy(userToken.messageBytes, 0, readyMessage, userToken.prefixBytes.Length, userToken.messageBytes.Length);
                return 0;
            }
            else
            {
                if (userToken.messageBytes.Length > 200)
                {
                    // гипотетически ответ не больше 200 байт, нужно закрыть сокет ином случае
                    return -1;
                }
            }

            return 0;
        }
    }
}
