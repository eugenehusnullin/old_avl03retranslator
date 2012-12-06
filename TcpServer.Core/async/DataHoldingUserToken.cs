using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace TcpServer.Core.async
{
    class DataHoldingUserToken
    {
        public int bufferOffsetReceive;
        public int bufferOffsetSend;

        
        public Byte[] byteArrayForPrefix;
        public int lengthOfCurrentIncomingMessage;
        public int receivedPrefixBytesDoneCount = 0;
        public int receivedMessageBytesDoneCount = 0;
        public int receiveMessageOffset;

        internal void reset()
        {
            throw new NotImplementedException();
        }
    }
}
