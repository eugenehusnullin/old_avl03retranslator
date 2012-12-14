using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Net.Sockets;
using System.Text;
using System.Threading.Tasks;

namespace TcpServer.Core.async
{
    class BlockConnector : BaseConnector
    {
        private const int BACKLOG = 500;
        private const int BUFFER_SIZE = 512;

        private EventHandler<SocketAsyncEventArgs> blockAcceptEventHandler;
        private EventHandler<SocketAsyncEventArgs> blockReceiveEventHandler;
        private EventHandler<SocketAsyncEventArgs> blockSendEventHandler;

        private Socket listenSocket;
        private string listenHost;
        private int listenPort;

        private volatile bool shutdown = false;

        private ReceivePrefixHandler receivePrefixHandler;
        private ReceiveMessageHandler receiveMessageHandler;

        private AsyncRetranslator.MessageReady messageReady;
        
        private ConcurrentQueue<KeyValuePair<byte[], SocketAsyncEventArgs>> blockSendQueue;
        private List<SendWorker> listSendToBlockWorkers;
        private readonly int cntSendToBlockWorkers = 15;

        public BlockConnector(string listenHost, int listenPort, AsyncRetranslator.MessageReady messageReady)
        {
            this.listenHost = listenHost;
            this.listenPort = listenPort;

            this.messageReady = messageReady;

            receivePrefixHandler = new ReceivePrefixHandler();
            receiveMessageHandler = new ReceiveMessageHandler();

            blockAcceptEventHandler = new EventHandler<SocketAsyncEventArgs>(blockAcceptEvent);
            blockReceiveEventHandler = new EventHandler<SocketAsyncEventArgs>(blockReceiveEvent);
            blockSendEventHandler = new EventHandler<SocketAsyncEventArgs>(blockSendEvent);

            blockSendQueue = new ConcurrentQueue<KeyValuePair<byte[], SocketAsyncEventArgs>>();
            listSendToBlockWorkers = new List<SendWorker>();
        }

        public void start()
        {
            for (int i = 0; i < cntSendToBlockWorkers; i++)
            {
                SendWorker w = new SendWorker(blockSendQueue, this);
                w.start();
                listSendToBlockWorkers.Add(w);
            }

            var listenIPAddress = IPAddress.Parse(listenHost);
            var localEndPoint = new IPEndPoint(listenIPAddress, listenPort);

            listenSocket = new Socket(localEndPoint.AddressFamily, SocketType.Stream, ProtocolType.Tcp);
            listenSocket.Bind(localEndPoint);
            listenSocket.Listen(BACKLOG);

            blockStartAccept();
        }

        public void stop()
        {
            shutdown = true;

            if (listenSocket.Connected)
            {
                listenSocket.Shutdown(SocketShutdown.Both);
            }
            listenSocket.Close();

            foreach (SendWorker w in listSendToBlockWorkers)
            {
                w.stop();
            }
        }

        private void blockAcceptEvent(object sender, SocketAsyncEventArgs saea)
        {
            blockProcessAccept(saea);
        }

        private void blockReceiveEvent(object sender, SocketAsyncEventArgs saea)
        {
            blockProcessReceive(saea);
        }

        private void blockSendEvent(object sender, SocketAsyncEventArgs saea)
        {
            blockProcessSend(saea);
        }

        private void blockStartAccept()
        {
            SocketAsyncEventArgs saea = new SocketAsyncEventArgs();
            saea.Completed += blockAcceptEventHandler;

            if (listenSocket != null && listenSocket.IsBound)
            {
                if (!listenSocket.AcceptAsync(saea))
                {
                    blockProcessAccept(saea);
                }
            }
        }

        private void blockProcessAccept(SocketAsyncEventArgs saea)
        {
            if (saea.SocketError == SocketError.OperationAborted && shutdown)
            {
                return;
            }
            else
            {
                blockStartAccept();

                if (saea.SocketError == SocketError.Success)
                {
                    SocketAsyncEventArgs rSaea = createSaea(blockReceiveEventHandler, BUFFER_SIZE);
                    var userToken = (DataHoldingUserToken)rSaea.UserToken;
                    userToken.socketGroup = new SocketGroup();
                    userToken.socketGroup.blockReceiveSAEA = saea;

                    rSaea.AcceptSocket = saea.AcceptSocket;
                    saea.AcceptSocket = null;

                    blockStartReceive(rSaea);
                }
                else
                {
                    saea.AcceptSocket.Close();
                }
            }
        }

        private void blockStartReceive(SocketAsyncEventArgs saea)
        {
            var userToken = (DataHoldingUserToken)saea.UserToken;
            userToken.resetVariableForNewRequest();

            if (!saea.AcceptSocket.ReceiveAsync(saea))
            {
                blockProcessReceive(saea);
            }
        }

        private void blockProcessReceive(SocketAsyncEventArgs saea)
        {
            var userToken = (DataHoldingUserToken)saea.UserToken;

            if (saea.SocketError != SocketError.Success || saea.BytesTransferred == 0)
            {
                userToken.reset();
                blockReceiveCloseSocket(saea, userToken.socketGroup);
                return;
            }

            int bytesToProcess = saea.BytesTransferred;
            while (bytesToProcess > 0)
            {
                bytesToProcess = receivePrefixHandler.handlePrefix(saea, userToken, bytesToProcess);
                if (bytesToProcess < 0)
                {
                    userToken.reset();
                    blockReceiveCloseSocket(saea, userToken.socketGroup);
                    return;
                }
                else if (bytesToProcess == 0)
                {
                    blockStartReceive(saea);
                    return;
                }

                byte[] message;
                bytesToProcess = receiveMessageHandler.handleMessage(saea, userToken, bytesToProcess, out message);
                if (message != null)
                {
                    messageReady(message, userToken.socketGroup);
                }
            }

            blockStartReceive(saea);
        }

        public void blockStartSend(SocketAsyncEventArgs saea, byte[] bytes)
        {
            if (!saea.AcceptSocket.Connected)
            {
                return;
            }

            var userToken = (DataHoldingUserToken)saea.UserToken;

            userToken.socketGroup.waitWhileSendToBlock.WaitOne();

            userToken.reset();
            userToken.messageBytes = bytes;
            blockStartSend(saea);
        }

        private void blockStartSend(SocketAsyncEventArgs saea)
        {
            var userToken = (DataHoldingUserToken)saea.UserToken;
            var socketGroup = userToken.socketGroup;

            Buffer.BlockCopy(userToken.messageBytes, userToken.messageBytesDoneCount, saea.Buffer,
                    saea.Offset, userToken.messageBytes.Length - userToken.messageBytesDoneCount);
            saea.SetBuffer(saea.Offset, userToken.messageBytes.Length - userToken.messageBytesDoneCount);
            if (!saea.AcceptSocket.SendAsync(saea))
            {
                blockProcessSend(saea);
            }
        }

        private void blockProcessSend(SocketAsyncEventArgs saea)
        {
            var userToken = (DataHoldingUserToken)saea.UserToken;

            if (saea.SocketError == SocketError.Success)
            {
                userToken.messageBytesDoneCount += saea.BytesTransferred;

                if (userToken.messageBytesDoneCount == userToken.messageBytes.Length)
                {
                    // отправка завершена
                    userToken.reset();
                    userToken.socketGroup.waitWhileSendToBlock.Set();
                }
                else
                {
                    blockStartSend(saea);
                }
            }
            else
            {
                blockSendCloseSocket(saea, userToken.socketGroup);
                userToken.socketGroup.waitWhileSendToBlock.Set();
            }
        }

        private void blockSendCloseSocket(SocketAsyncEventArgs saea, SocketGroup socketGroup)
        {
            try
            {
                if (saea.AcceptSocket.Connected)
                {
                    saea.AcceptSocket.Shutdown(SocketShutdown.Both);
                }
                saea.AcceptSocket.Close();

                if (socketGroup.monReceiveSAEA.AcceptSocket.Connected)
                {
                    socketGroup.monReceiveSAEA.AcceptSocket.Shutdown(SocketShutdown.Both);
                }
                socketGroup.monReceiveSAEA.AcceptSocket.Close();
            }
            catch
            {
            }
        }

        public void blockReceiveCloseSocket(SocketAsyncEventArgs saea, SocketGroup socketGroup)
        {
            try
            {
                if (socketGroup.monSendSAEA != null)
                {
                    if (socketGroup.monSendSAEA.AcceptSocket.Connected)
                    {
                        socketGroup.monSendSAEA.AcceptSocket.Shutdown(SocketShutdown.Both);
                    }

                    socketGroup.monSendSAEA.AcceptSocket.Close();
                }

                if (saea.AcceptSocket.Connected)
                {
                    saea.AcceptSocket.Shutdown(SocketShutdown.Both);
                }
                saea.AcceptSocket.Close();
            }
            catch
            {
            }
        }

        internal void setSendSaea(SocketGroup socketGroup)
        {
            if (socketGroup.blockSendSAEA == null)
            {
                socketGroup.blockSendSAEA = createSaea(blockSendEventHandler, BUFFER_SIZE);
                socketGroup.blockSendSAEA.AcceptSocket = socketGroup.blockReceiveSAEA.AcceptSocket;
                ((DataHoldingUserToken)socketGroup.blockSendSAEA.UserToken).socketGroup = socketGroup;
            }
        }

        internal void enqueueForSend(KeyValuePair<byte[], SocketAsyncEventArgs> keyValuePair)
        {
            blockSendQueue.Enqueue(keyValuePair);
        }

        public override void startSend(SocketAsyncEventArgs saea, byte[] bytes)
        {
            blockStartSend(saea, bytes);
        }
    }
}
