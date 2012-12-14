using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Sockets;
using System.Text;
using System.Threading.Tasks;

namespace TcpServer.Core.async
{
    class ReceiveMessageHandler
    {
        public delegate void ProccessMessage(string prefix, string message, SocketGroup socketGroup);

        public static int handleMessage(SocketAsyncEventArgs rs, DataHoldingUserToken userToken, int bytesToProcess, ProccessMessage processMessageCallback)
        {
            if (userToken.messageBytesDoneCount == 0)
            {
                userToken.messageBytes = new byte[userToken.messageLength];
            }

            int length = Math.Min(userToken.messageLength - userToken.messageBytesDoneCount, bytesToProcess);

            Buffer.BlockCopy(rs.Buffer, rs.Offset +
                userToken.prefixBytesDoneCountThisOp + userToken.messageBytesDoneCountThisOp,
                userToken.messageBytes, userToken.messageBytesDoneCount, length);

            userToken.messageBytesDoneCount += length;
            userToken.messageBytesDoneCountThisOp += length;

            if (userToken.messageBytesDoneCount == userToken.messageLength)
            {
                //// сообщение готово
                var prefix = Encoding.ASCII.GetString(userToken.prefixBytes);
                var message = Encoding.ASCII.GetString(userToken.messageBytes);
                processMessageCallback(prefix, message, userToken.socketGroup);

                userToken.reset();
            }

            return bytesToProcess - length;
        }
    }
}
