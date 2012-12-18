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
            if (bytesToProcess == 0 || userToken.prefixBytesDoneCount >= RECEIVE_PREFIX_LENGTH)
            {
                return bytesToProcess;
            }

            if (userToken.prefixBytesDoneCount == 0)
            {
                userToken.prefixBytes = new byte[RECEIVE_PREFIX_LENGTH];
            }

            int length = Math.Min(RECEIVE_PREFIX_LENGTH - userToken.prefixBytesDoneCount, bytesToProcess);

            Buffer.BlockCopy(rs.Buffer, rs.Offset + userToken.bytesDoneCountThisOp,
                userToken.prefixBytes, userToken.prefixBytesDoneCount, length);

            userToken.prefixBytesDoneCount += length;
            userToken.bytesDoneCountThisOp += length;


            return bytesToProcess - length;
        }
    }
}
