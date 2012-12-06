using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Net.Sockets;
using System.Text;

namespace TcpServer.Core.async
{
    class ConnectionsAccepter
    {
        private const int INIT_ACCEPT_POOL_SIZE = 50;
        private const int BACKLOG = 500;
        private const int INIT_BLOCK_POOL_SIZE = 1000;
        private const int RECEIVE_BUFFER_SIZE = 512;
        private const int SEND_BUFFER_SIZE = 512;
        private const int MAX_BUFFERS_COUNT = 10;
        private const int RECEIVE_PREFIX_LENGTH = 4;

        private Socket listenSocket;
        private string listenHost;
        private int listenPort;

        private Stack<SocketAsyncEventArgs> acceptPool;
        private static object acceptPoolLock = new object();
        private Stack<SocketAsyncEventArgs> blockPool;
        private static object blockPoolLock = new object();
        private Stack<SocketAsyncEventArgs> monPool;
                
        private byte[][] bufferArrays;
        private int currentBufferArrayIndex = 0;
        private int currentBufferOffset = 0;
        private static object bufferLock = new object();
        private int initBufferSize;

        private void init()
        {
            initBufferSize = (RECEIVE_BUFFER_SIZE * INIT_BLOCK_POOL_SIZE) + (SEND_BUFFER_SIZE + INIT_BLOCK_POOL_SIZE);

            bufferArrays = new byte[MAX_BUFFERS_COUNT][];
            bufferArrays[currentBufferArrayIndex] = new byte[initBufferSize];

            acceptPool = new Stack<SocketAsyncEventArgs>();
            for (int i = 0; i < INIT_ACCEPT_POOL_SIZE; i++)
            {
                acceptPool.Push(createSocketAsyncEventArgsForAccept());
            }

            blockPool = new Stack<SocketAsyncEventArgs>();
            for (int i = 0; i < INIT_BLOCK_POOL_SIZE; i++)
            {
                blockPool.Push(createSocketAsyncEventArgsForBlock());
            }
        }

        private SocketAsyncEventArgs createSocketAsyncEventArgsForBlock()
        {
            SocketAsyncEventArgs s = new SocketAsyncEventArgs();
            s.Completed += new EventHandler<SocketAsyncEventArgs>(blockEvent);

            DataHoldingUserToken userToken = new DataHoldingUserToken();

            lock (bufferLock)
            {
                if (currentBufferOffset == initBufferSize)
                {
                    if (currentBufferArrayIndex == MAX_BUFFERS_COUNT)
                    {
                        throw new Exception("Please increase MAX_BUFFERS_COUNT settings.");
                    }

                    currentBufferArrayIndex++;
                    bufferArrays[currentBufferArrayIndex] = new byte[initBufferSize];
                    currentBufferOffset = 0;
                }                
                s.SetBuffer(bufferArrays[currentBufferArrayIndex], currentBufferOffset, 0);
                
                userToken.bufferOffsetReceive = currentBufferOffset;
                currentBufferOffset += RECEIVE_BUFFER_SIZE;
                userToken.bufferOffsetSend = currentBufferOffset;
                currentBufferOffset += SEND_BUFFER_SIZE;
            }

            s.UserToken = userToken;
            return s;
        }

        private SocketAsyncEventArgs createSocketAsyncEventArgsForAccept()
        {
            SocketAsyncEventArgs s = new SocketAsyncEventArgs();
            s.Completed += new EventHandler<SocketAsyncEventArgs>(acceptEvent);

            return s;
        }

        public void start()
        {
            init();

            var listenIPAddress = IPAddress.Parse(listenHost);
            IPEndPoint localEndPoint = new IPEndPoint(listenIPAddress, listenPort);

            listenSocket = new Socket(localEndPoint.AddressFamily, SocketType.Stream, ProtocolType.Tcp);
            listenSocket.Bind(localEndPoint);
            listenSocket.Listen(BACKLOG);

            startAccept();            
        }

        private void startAccept()
        {
            SocketAsyncEventArgs s;
            lock (acceptPoolLock)
            {
                if (acceptPool.Count == 0)
                {
                    s = createSocketAsyncEventArgsForAccept();
                }
                else
                {
                    s = acceptPool.Pop();
                }                
            }

            if (!listenSocket.AcceptAsync(s))
            {
                processAccept(s);
            }
        }

        private void processAccept(SocketAsyncEventArgs s)
        {
            startAccept();

            if (s.SocketError != SocketError.Success)
            {
                s.AcceptSocket.Close();
                lock (acceptPoolLock)
                {
                    acceptPool.Push(s);
                }
                return;
            }

            SocketAsyncEventArgs rs;
            lock(blockPoolLock)
            {
                if (blockPool.Count == 0)
                {
                    rs = createSocketAsyncEventArgsForBlock();
                }
                else
                {
                    rs = blockPool.Pop();
                }
            }

            rs.AcceptSocket = s.AcceptSocket;
            s.AcceptSocket = null;
            
            startReceive(rs);

            lock (acceptPoolLock)
            {
                acceptPool.Push(s);
            }
        }

        private void acceptEvent(object sender, SocketAsyncEventArgs e)
        {
            processAccept(e);
        }

        private void startReceive(SocketAsyncEventArgs rs)
        {
            DataHoldingUserToken userToken = (DataHoldingUserToken)rs.UserToken;
            rs.SetBuffer(userToken.bufferOffsetReceive, RECEIVE_BUFFER_SIZE);

            if (!rs.AcceptSocket.ReceiveAsync(rs))
            {
                processReceive(rs);
            }
        }

        private void processReceive(SocketAsyncEventArgs rs)
        {
            var userToken = (DataHoldingUserToken) rs.UserToken;

            if (rs.SocketError != SocketError.Success || rs.BytesTransferred == 0)
            {
                userToken.reset();
                closeSocket(rs);
                return;
            }

            int bytesToProcess = rs.BytesTransferred;

            if (userToken.receivedPrefixBytesDoneCount < RECEIVE_PREFIX_LENGTH)
            {
                bytesToProcess = handlePrefix(rs, userToken, bytesToProcess);

                if (bytesToProcess == 0)
                {
                    startReceive(rs);
                    return;
                }
            }
            else
            {
            }
        }

        

        private void blockEvent(object sender, SocketAsyncEventArgs e)
        {
            switch (e.LastOperation)
            {
                case SocketAsyncOperation.Receive:
                    processReceive(e);
                    break;

                case SocketAsyncOperation.Send:
                    processSend(e);
                    break;

                default:
                    throw new Exception("The last operation completed on the socket was not a receive or send");
            }
        }

        private void processSend(SocketAsyncEventArgs e)
        {
            throw new NotImplementedException();
        }

        private void closeSocket(SocketAsyncEventArgs e)
        {
            try
            {
                e.AcceptSocket.Shutdown(SocketShutdown.Both);
            }
            catch (Exception)
            {
            }

            e.AcceptSocket.Close();

            lock (blockPoolLock)
            {
                blockPool.Push(e);
            }
        }

        private int handlePrefix(SocketAsyncEventArgs rs, DataHoldingUserToken userToken, int bytesToProcess)
        {
            throw new NotImplementedException();
        }
    }
}
