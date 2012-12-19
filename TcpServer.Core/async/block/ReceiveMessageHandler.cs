using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Sockets;
using System.Text;
using System.Threading.Tasks;
using TcpServer.Core.async.common;

namespace TcpServer.Core.async.block
{
    class ReceiveMessageHandler
    {
        public int handleMessage(SocketAsyncEventArgs saea, DataHoldingUserToken userToken, int bytesToProcess, out byte[] readyMessage)
        {
            if (userToken.messageBytesDoneCount == 0)
            {
                userToken.messageBytes = new byte[userToken.messageLength];
            }

            int length = Math.Min(userToken.messageLength - userToken.messageBytesDoneCount, bytesToProcess);

            Buffer.BlockCopy(saea.Buffer, saea.Offset + userToken.bytesDoneThisOp,
                userToken.messageBytes, userToken.messageBytesDoneCount, length);

            userToken.messageBytesDoneCount += length;
            userToken.bytesDoneThisOp += length;

            readyMessage = null;
            if (userToken.messageBytesDoneCount == userToken.messageLength)
            {
                //// сообщение готово
                readyMessage = new byte[userToken.prefixBytes.Length + userToken.messageBytes.Length];
                Buffer.BlockCopy(userToken.prefixBytes, 0, readyMessage, 0, userToken.prefixBytes.Length);
                Buffer.BlockCopy(userToken.messageBytes, 0, readyMessage, userToken.prefixBytes.Length, userToken.messageBytes.Length);
            }
            //return bytesToProcess - length;
            return 0;
        }
    }
}
