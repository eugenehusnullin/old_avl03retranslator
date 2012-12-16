using log4net;
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Net.Sockets;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using TcpServer.Core.async.common;
using TcpServer.Core.async.retranslator;

namespace TcpServer.Core.async.mon
{
    class MonConnector : BaseConnector
    {
        private const int ATTEMPT_CONNECT_TO_MONITORING = 3;
        private const int TIMEOUT_BETWEEN_ATTEMPT_CONNECT_TO_MONITORING = 7000;
        private const int BUFFER_SIZE = 512;

        private ILog log;

        private EventHandler<SocketAsyncEventArgs> receiveEventHandler;
        private EventHandler<SocketAsyncEventArgs> sendEventHandler;

        private string monHost;
        private int monPort;
        private IPEndPoint monEndPoint;

        public MonConnector(string monHost, int monPort, MessageReceived messageReceived, BaseConnector.MessageSended messageSended,
            ConnectionFailed connectionFailed)
        {
            this.monHost = monHost;
            this.monPort = monPort;
            var monIPAddress = IPAddress.Parse(monHost);
            monEndPoint = new IPEndPoint(monIPAddress, monPort);

            this.messageReceived = messageReceived;
            this.messageSended = messageSended;
            this.connectionFailed = connectionFailed;

            sendEventHandler = new EventHandler<SocketAsyncEventArgs>(sendEvent);
            receiveEventHandler = new EventHandler<SocketAsyncEventArgs>(receiveEvent);

            log = LogManager.GetLogger(typeof(MonConnector));
        }

        private void receiveEvent(object sender, SocketAsyncEventArgs saea)
        {
            processReceive(saea);
        }

        private void sendEvent(object sender, SocketAsyncEventArgs saea)
        {
            processSend(saea);
        }

        public void startReceive(SocketAsyncEventArgs saea)
        {
            if (!saea.AcceptSocket.ReceiveAsync(saea))
            {
                processReceive(saea);
            }
        }

        private void processReceive(SocketAsyncEventArgs saea)
        {
            var userToken = (DataHoldingUserToken)saea.UserToken;

            if (saea.SocketError != SocketError.Success || saea.BytesTransferred == 0)
            {
                closeSocketInner(saea);
            }
            else
            {
                byte[] bytes = new byte[saea.BytesTransferred];
                Buffer.BlockCopy(saea.Buffer, saea.Offset, bytes, 0, saea.BytesTransferred);
                userToken.resetAll();
                messageReceived(bytes, saea);
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

        public bool createConnection(out SocketAsyncEventArgs monReceive, out SocketAsyncEventArgs monSend)
        {
            monReceive = null;
            monSend = null;

            Socket socket = new Socket(AddressFamily.InterNetwork, SocketType.Stream, ProtocolType.Tcp);
            int attempt = 0;
            while (!socket.Connected && attempt < ATTEMPT_CONNECT_TO_MONITORING)
            {
                if (attempt > 0)
                {
                    Thread.Sleep(TIMEOUT_BETWEEN_ATTEMPT_CONNECT_TO_MONITORING);
                }
                attempt++;
                try
                {
                    socket.Connect(monEndPoint);
                }
                catch
                {
                }
            }
            if (!socket.Connected)
            {
                log.Error("Cannot establish connection to monitoring.");
                return false;
            }

            monReceive = createSaea(receiveEventHandler, BUFFER_SIZE);
            monReceive.AcceptSocket = socket;

            monSend = createSaea(sendEventHandler, BUFFER_SIZE);
            monSend.AcceptSocket = socket;

            startReceive(monReceive);

            return true;
        }
    }
}
