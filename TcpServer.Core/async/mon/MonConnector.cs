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
    public class MonConnector : BaseConnector
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
            ReceiveFailed receiveFailed, SendFailed sendFailed)
        {
            this.monHost = monHost;
            this.monPort = monPort;
            var monIPAddress = IPAddress.Parse(monHost);
            monEndPoint = new IPEndPoint(monIPAddress, monPort);

            this.messageReceived = messageReceived;
            this.messageSended = messageSended;
            this.receiveFailed = receiveFailed;
            this.sendFailed = sendFailed;

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
            try
            {
                if (!saea.AcceptSocket.ReceiveAsync(saea))
                {
                    processReceive(saea);
                }
            }
            catch (Exception e)
            {
                log.Debug("Start receive from monitoring failed.", e);
                receiveFailed(saea);
                closeSocket(saea);
                (saea.UserToken as DataHoldingUserToken).socketGroup = null;
                saea.Dispose();
            }
        }

        private void processReceive(SocketAsyncEventArgs saea)
        {
            var userToken = (DataHoldingUserToken)saea.UserToken;

            if (saea.SocketError != SocketError.Success || saea.BytesTransferred == 0)
            {
                log.DebugFormat("Process receive from monitoring failed. SocketError={0} or BytesTransferred={1}.", saea.SocketError, saea.BytesTransferred);
                receiveFailed(saea);
                closeSocket(saea);
                userToken.socketGroup = null;
                saea.Dispose();
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
            catch (Exception e)
            {
                log.Debug("Start send to monitoring failed.", e);
                sendFailed(saea);                
                closeSocket(saea);
                userToken.socketGroup = null;
                saea.Dispose();
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
                log.DebugFormat("Process send to monitoring failed. SocketError={0}", saea.SocketError);
                userToken.resetAll();
                sendFailed(saea);
                closeSocket(saea);
                userToken.socketGroup = null;
                saea.Dispose();
            }
        }

        public void closeSocket(SocketAsyncEventArgs saea)
        {
            log.Debug("Close socket to monitoring.");
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

        public bool createConnection(out SocketAsyncEventArgs receiveSaea, out SocketAsyncEventArgs sendSaea)
        {
            receiveSaea = null;
            sendSaea = null;

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
                catch(Exception e)
                {
                    log.Error("Cannot establish connection to monitoring.", e);
                }
            }
            if (!socket.Connected)
            {
                return false;
            }

            socket.ReceiveTimeout = 5 * 60 * 1000;
            socket.SendTimeout = 1 * 60 * 1000;

            receiveSaea = createSaea(receiveEventHandler, BUFFER_SIZE);
            receiveSaea.AcceptSocket = socket;

            sendSaea = createSaea(sendEventHandler, BUFFER_SIZE);
            sendSaea.AcceptSocket = socket;

            return true;
        }
    }
}
