using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Sockets;
using System.Text;
using TcpServer.Core.async.block;

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

        public int bytesDoneThisOp = 0;

        public int dataTypeId = 0;

        public UImageHolder uImageHolder = new UImageHolder();

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
            bytesDoneThisOp = 0;
        }

        public void resetAll()
        {
            resetReadyMessage();
            resetVariableForNewRequest();
        }
    }
}
