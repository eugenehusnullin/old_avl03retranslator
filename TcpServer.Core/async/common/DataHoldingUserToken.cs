using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Sockets;
using System.Text;

namespace TcpServer.Core.async.common
{
    public class DataHoldingUserToken
    {
        public SocketGroup socketGroup;
        
        public Byte[] prefixBytes;
        public Byte[] messageBytes;

        public int prefixBytesDoneCount = 0;        
        public int messageBytesDoneCount = 0;
        public int messageLength = 0;

        public int bytesDoneCountThisOp = 0;

        public int dataTypeId = 0;

        public DataHoldingUserToken()
        {
        }

        public void resetReadyMessage()
        {
            prefixBytes = null;
            messageBytes = null;

            prefixBytesDoneCount = 0;
            messageBytesDoneCount = 0;
            messageLength = 0;

            dataTypeId = 0;
        }

        public void resetVariableForNewRequest()
        {
            bytesDoneCountThisOp = 0;
        }

        public void resetAll()
        {
            resetReadyMessage();
            resetVariableForNewRequest();
        }
    }
}
