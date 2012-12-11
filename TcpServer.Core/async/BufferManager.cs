using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Net.Sockets;
using System.Text;

namespace TcpServer.Core.async
{
    class BufferManager
    {
        private readonly int bufferCount;
        private readonly int bufferSize;

        private List<byte[]> listByteBuffers;
        private volatile byte[] currentByteBuffer;
        private volatile int currentByteBufferOffset = 0;
        private static object bufferLock = new object();


        public BufferManager(int bufferCount, int bufferSize)
        {
            this.bufferCount = bufferCount;
            this.bufferSize = bufferSize;
            listByteBuffers = new List<byte[]>();
            setNewByteBuffer();
        }

        public void setBuffer(SocketAsyncEventArgs saea)
        {
            lock (bufferLock)
            {
                if (currentByteBufferOffset == bufferCount * bufferSize)
                {
                    setNewByteBuffer();
                }

                saea.SetBuffer(currentByteBuffer, currentByteBufferOffset, bufferSize);
                currentByteBufferOffset += bufferSize;
            }
        }

        private void setNewByteBuffer()
        {
            currentByteBuffer = new byte[bufferCount * bufferSize];
            listByteBuffers.Add(currentByteBuffer);
            currentByteBufferOffset = 0;
        }
    }
}
