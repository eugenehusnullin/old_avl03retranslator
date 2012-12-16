using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Net.Sockets;
using System.Text;
using System.Threading.Tasks;
using TcpServer.Core.async.common;

namespace TcpServer.Core.async.block
{
    class BlocksAcceptor : BaseConnector
    {
        private const int BACKLOG = 500;
        private const int BUFFER_SIZE = 512;

        public delegate void ConnectionAccepted(SocketAsyncEventArgs saea);
        private ConnectionAccepted connectionAccepted;

        private EventHandler<SocketAsyncEventArgs> blockAcceptEventHandler;
        private EventHandler<SocketAsyncEventArgs> receiveEventHandler;
        private EventHandler<SocketAsyncEventArgs> blockSendEventHandler;

        private Socket listenSocket;
        private string listenHost;
        private int listenPort;

        private volatile bool shutdown = false;

        private ReceivePrefixHandler receivePrefixHandler;
        private ReceiveMessageHandler receiveMessageHandler;

        public BlocksAcceptor(string listenHost, int listenPort, MessageReceived messageReceived, MessageSended messageSended,
            ConnectionFailed connectionFailed, ConnectionAccepted connectionAccepted)
        {
            this.listenHost = listenHost;
            this.listenPort = listenPort;

            this.messageReceived = messageReceived;
            this.messageSended = messageSended;
            this.connectionFailed = connectionFailed;
            this.connectionAccepted = connectionAccepted;

            receivePrefixHandler = new ReceivePrefixHandler();
            receiveMessageHandler = new ReceiveMessageHandler();

            blockAcceptEventHandler = new EventHandler<SocketAsyncEventArgs>(acceptEvent);
            receiveEventHandler = new EventHandler<SocketAsyncEventArgs>(receiveEvent);
            blockSendEventHandler = new EventHandler<SocketAsyncEventArgs>(sendEvent);
        }

        public void start()
        {
            var listenIPAddress = IPAddress.Parse(listenHost);
            var localEndPoint = new IPEndPoint(listenIPAddress, listenPort);

            listenSocket = new Socket(localEndPoint.AddressFamily, SocketType.Stream, ProtocolType.Tcp);
            listenSocket.Bind(localEndPoint);
            listenSocket.Listen(BACKLOG);

            startAccept();
        }

        public void stop()
        {
            shutdown = true;

            if (listenSocket.Connected)
            {
                listenSocket.Shutdown(SocketShutdown.Both);
            }
            listenSocket.Close();
        }

        private void acceptEvent(object sender, SocketAsyncEventArgs saea)
        {
            processAccept(saea);
        }

        private void receiveEvent(object sender, SocketAsyncEventArgs saea)
        {
            processReceive(saea);
        }

        private void sendEvent(object sender, SocketAsyncEventArgs saea)
        {
            processSend(saea);
        }

        private void startAccept()
        {
            SocketAsyncEventArgs saea = new SocketAsyncEventArgs();
            saea.Completed += blockAcceptEventHandler;

            if (listenSocket != null && listenSocket.IsBound)
            {
                if (!listenSocket.AcceptAsync(saea))
                {
                    processAccept(saea);
                }
            }
        }

        private void processAccept(SocketAsyncEventArgs saea)
        {
            if (saea.SocketError == SocketError.OperationAborted && shutdown)
            {
                return;
            }
            else
            {
                startAccept();

                if (saea.SocketError == SocketError.Success)
                {
                    SocketAsyncEventArgs receiveSaea = createSaea(receiveEventHandler, BUFFER_SIZE);
                    receiveSaea.AcceptSocket = saea.AcceptSocket;
                    connectionAccepted(receiveSaea);
                    startReceive(receiveSaea);

                    saea.AcceptSocket = null;
                }
                else
                {
                    saea.AcceptSocket.Close();
                }
            }
        }

        public void startReceive(SocketAsyncEventArgs saea)
        {
            var userToken = (DataHoldingUserToken)saea.UserToken;

            if ((userToken.prefixBytesDoneCountThisOp + userToken.messageBytesDoneCountThisOp) == saea.BytesTransferred)
            {
                userToken.resetVariableForNewRequest();
                if (!saea.AcceptSocket.ReceiveAsync(saea))
                {
                    processReceive(saea);
                }
            }
            else
            {
                processReceive(saea);
            }
        }

        private void processReceive(SocketAsyncEventArgs saea)
        {
            var userToken = (DataHoldingUserToken)saea.UserToken;

            if (saea.SocketError != SocketError.Success || saea.BytesTransferred == 0)
            {
                userToken.resetAll();
                closeSocketInner(saea);
                return;
            }

            int bytesToProcess = saea.BytesTransferred - (userToken.prefixBytesDoneCountThisOp + userToken.messageBytesDoneCountThisOp);
            bytesToProcess = receivePrefixHandler.handlePrefix(saea, userToken, bytesToProcess);
            if (bytesToProcess < 0)
            {
                userToken.resetAll();
                closeSocketInner(saea);
                return;
            }
            else if (bytesToProcess == 0)
            {
                startReceive(saea);
                return;
            }
            else
            {
                byte[] message = receiveMessageHandler.handleMessage(saea, userToken, bytesToProcess);
                if (message != null)
                {
                    userToken.resetReadyMessage();
                    messageReceived(message, saea);
                    return;
                }
                else
                {
                    startReceive(saea);
                    return;
                }
            }
        }

        public void startSend(SocketAsyncEventArgs saea, byte[] bytes)
        {
            var userToken = (DataHoldingUserToken)saea.UserToken;
            userToken.resetAll();
            userToken.messageBytes = bytes;
            startSend(saea);
        }

        private void startSend(SocketAsyncEventArgs saea)
        {
            var userToken = (DataHoldingUserToken)saea.UserToken;

            Buffer.BlockCopy(userToken.messageBytes, userToken.messageBytesDoneCount, saea.Buffer,
                    saea.Offset, userToken.messageBytes.Length - userToken.messageBytesDoneCount);
            saea.SetBuffer(saea.Offset, userToken.messageBytes.Length - userToken.messageBytesDoneCount);
            try
            {
                if (!saea.AcceptSocket.SendAsync(saea))
                {
                    processSend(saea);
                }
            }
            catch
            {
                closeSocketInner(saea);
            }
        }

        private void processSend(SocketAsyncEventArgs saea)
        {
            var userToken = (DataHoldingUserToken)saea.UserToken;

            if (saea.SocketError == SocketError.Success)
            {
                userToken.messageBytesDoneCount += saea.BytesTransferred;

                if (userToken.messageBytesDoneCount == userToken.messageBytes.Length)
                {
                    // отправка завершена
                    userToken.resetAll();
                    messageSended(saea);
                }
                else
                {
                    startSend(saea);
                }
            }
            else
            {
                userToken.resetAll();
                closeSocketInner(saea);
            }
        }

        private void closeSocketInner(SocketAsyncEventArgs saea)
        {
            closeSocket(saea);
            connectionFailed(saea);
        }

        public void closeSocket(SocketAsyncEventArgs saea)
        {
            try
            {
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

        public SocketAsyncEventArgs createSaeaForSend(Socket acceptSocket)
        {
            SocketAsyncEventArgs saea = createSaea(blockSendEventHandler, BUFFER_SIZE);
            saea.AcceptSocket = acceptSocket;
            return saea;
        }
    }
}
