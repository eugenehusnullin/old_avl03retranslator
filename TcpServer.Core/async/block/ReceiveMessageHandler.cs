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
        public byte[] handleMessage(SocketAsyncEventArgs rs, DataHoldingUserToken userToken, int bytesToProcess)
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

            byte[] message = null;
            if (userToken.messageBytesDoneCount == userToken.messageLength)
            {
                //// сообщение готово
                message = new byte[userToken.prefixBytes.Length + userToken.messageBytes.Length];
                Buffer.BlockCopy(userToken.prefixBytes, 0, message, 0, userToken.prefixBytes.Length);
                Buffer.BlockCopy(userToken.messageBytes, 0, message, userToken.prefixBytes.Length, userToken.messageBytes.Length);
            }
            //return bytesToProcess - length;
            return message;
        }
    }
}
