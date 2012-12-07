using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Sockets;
using System.Text;

namespace TcpServer.Core.async
{
    class DataHoldingUserToken
    {
        public readonly int bufferOffsetReceive;
        public readonly int bufferOffsetSend;
        
        public Byte[] byteArrayForPrefix;
        public Byte[] byteArrayForMessage;
        public int receivedPrefixBytesDoneCount = 0;        
        public int receivedMessageBytesDoneCount = 0;
        public int lengthOfCurrentIncomingMessage = 0;
        public int receivedPrefixBytesDoneCountThisOperation = 0;
        public int receivedMessageBytesDoneCountThisOperation = 0;

        public string receivedPrefix;
        public string receivedMessage;
        
        //public int receiveMessageOffset;
        //public readonly int permanentReceiveMessageOffset;

        public DataHoldingUserToken(int rOffset, int sOffset, int receivePrefixLength, int sendPrefixLength)
        {
            this.bufferOffsetReceive = rOffset;
            this.bufferOffsetSend = sOffset;
            //this.receiveMessageOffset = rOffset + receivePrefixLength;
            //this.permanentReceiveMessageOffset = this.receiveMessageOffset;
        }

        public void reset()
        {
            byteArrayForPrefix = null;
            byteArrayForMessage = null;

            receivedPrefixBytesDoneCount = 0;
            receivedMessageBytesDoneCount = 0;
            lengthOfCurrentIncomingMessage = 0;
            receivedPrefixBytesDoneCountThisOperation = 0;
            receivedMessageBytesDoneCountThisOperation = 0;
        }

        public void beforeNewRequestReceive()
        {
            receivedPrefixBytesDoneCountThisOperation = 0;
            receivedMessageBytesDoneCountThisOperation = 0;
        }
    }
}
