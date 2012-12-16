using log4net;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Net.Sockets;
using System.Text;
using System.Threading.Tasks;
using TcpServer.Core.async.common;

namespace TcpServer.Core.async.block
{
    class ReceivePrefixHandler
    {
        private const int RECEIVE_PREFIX_LENGTH = 4;

        private ILog log;

        public ReceivePrefixHandler()
        {
            log = LogManager.GetLogger(typeof(ReceivePrefixHandler));
        }

        public int handlePrefix(SocketAsyncEventArgs rs, DataHoldingUserToken userToken, int bytesToProcess)
        {
            if (userToken.prefixBytesDoneCount >= RECEIVE_PREFIX_LENGTH)
            {
                return bytesToProcess;
            }

            if (userToken.prefixBytesDoneCount == 0)
            {
                userToken.prefixBytes = new byte[RECEIVE_PREFIX_LENGTH];
            }

            int length = Math.Min(RECEIVE_PREFIX_LENGTH - userToken.prefixBytesDoneCount, bytesToProcess);

            Buffer.BlockCopy(rs.Buffer, rs.Offset
                + userToken.prefixBytesDoneCountThisOp + userToken.messageBytesDoneCountThisOp,
                userToken.prefixBytes, userToken.prefixBytesDoneCount, length);

            userToken.prefixBytesDoneCount += length;
            userToken.prefixBytesDoneCountThisOp += length;

            if (userToken.prefixBytesDoneCount == RECEIVE_PREFIX_LENGTH)
            {
                // заголовок готов, проверяем его, если он нормальный устанавливаем длину ожидаемого сообщения
                var prefix = Encoding.ASCII.GetString(userToken.prefixBytes);
                if (!prefix.StartsWith("$$"))
                {
                    log.WarnFormat("Someone sended us a bad packet with prefix={0} his IP={1}", prefix, ((IPEndPoint)rs.AcceptSocket.RemoteEndPoint).Address);
                    return -1;
                }

                try
                {
                    userToken.messageLength = Convert.ToInt32(prefix.Substring(2), 16) - 4;
                    if (userToken.messageLength <= 0)
                    {
                        return -2;
                    }
                }
                catch
                {
                    log.WarnFormat("Someone sended us a bad packet size prefix={0} his IP={1}", prefix, ((IPEndPoint)rs.AcceptSocket.RemoteEndPoint).Address);
                }
            }

            return bytesToProcess - length;
        }
    }
}
