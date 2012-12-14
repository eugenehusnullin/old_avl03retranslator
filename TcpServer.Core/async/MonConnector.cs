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

namespace TcpServer.Core.async
{
    class MonConnector : BaseConnector
    {
        private const int ATTEMPT_CONNECT_TO_MONITORING = 3;
        private const int TIMEOUT_BETWEEN_ATTEMPT_CONNECT_TO_MONITORING = 7000;
        private const int BUFFER_SIZE = 512;

        private ILog log;

        private EventHandler<SocketAsyncEventArgs> monReceiveEventHandler;
        private EventHandler<SocketAsyncEventArgs> monSendEventHandler;

        private string monHost;
        private int monPort;
        private IPEndPoint monEndPoint;

        private AsyncRetranslator.MessageReady messageReady;
        private ConcurrentQueue<KeyValuePair<byte[], SocketAsyncEventArgs>> monSendQueue;
        private List<SendWorker> listSendToMonWorkers;

        private readonly int cntSendToMonWorkers = 100;

        public MonConnector(string monHost, int monPort, AsyncRetranslator.MessageReady messageReady)
        {
            this.monHost = monHost;
            this.monPort = monPort;
            var monIPAddress = IPAddress.Parse(monHost);
            monEndPoint = new IPEndPoint(monIPAddress, monPort);

            this.messageReady = messageReady;

            monSendEventHandler = new EventHandler<SocketAsyncEventArgs>(monSendEvent);
            monReceiveEventHandler = new EventHandler<SocketAsyncEventArgs>(monReceiveEvent);

            log = LogManager.GetLogger(typeof(MonConnector));

            monSendQueue = new ConcurrentQueue<KeyValuePair<byte[], SocketAsyncEventArgs>>();
            listSendToMonWorkers = new List<SendWorker>();
        }

        internal void start()
        {
            for (int i = 0; i < cntSendToMonWorkers; i++)
            {
                SendWorker w = new SendWorker(monSendQueue, this);
                w.start();
                listSendToMonWorkers.Add(w);
            }
        }

        internal void stop()
        {
            foreach (SendWorker w in listSendToMonWorkers)
            {
                w.stop();
            }
        }

        private void monReceiveEvent(object sender, SocketAsyncEventArgs saea)
        {
            monProcessReceive(saea);
        }

        private void monSendEvent(object sender, SocketAsyncEventArgs saea)
        {
            monProcessSend(saea);
        }

        private void monStartReceive(SocketAsyncEventArgs saea)
        {
            var userToken = (DataHoldingUserToken)saea.UserToken;
            userToken.resetVariableForNewRequest();

            if (!saea.AcceptSocket.ReceiveAsync(saea))
            {
                monProcessReceive(saea);
            }
        }

        private void monProcessReceive(SocketAsyncEventArgs saea)
        {
            var userToken = (DataHoldingUserToken)saea.UserToken;
            var socketGroup = userToken.socketGroup;

            if (saea.SocketError != SocketError.Success || saea.BytesTransferred == 0)
            {
                monReceiveCloseSocket(saea, socketGroup);
            }
            else
            {
                byte[] bytes = new byte[saea.BytesTransferred];
                Buffer.BlockCopy(saea.Buffer, saea.Offset, bytes, 0, saea.BytesTransferred);
                messageReady(bytes, socketGroup);

                monStartReceive(saea);
            }
        }

        public static void monStartSend(SocketAsyncEventArgs saea, byte[] bytes)
        {
            if (!saea.AcceptSocket.Connected)
            {
                return;
            }

            var userToken = (DataHoldingUserToken)saea.UserToken;
            userToken.socketGroup.waitWhileSendToMon.WaitOne();

            userToken.reset();
            userToken.messageBytes = bytes;
            monStartSend(saea);
        }

        private static void monStartSend(SocketAsyncEventArgs saea)
        {
            var userToken = (DataHoldingUserToken)saea.UserToken;
            var socketGroup = userToken.socketGroup;

            Buffer.BlockCopy(userToken.messageBytes, userToken.messageBytesDoneCount, saea.Buffer,
                    saea.Offset, userToken.messageBytes.Length - userToken.messageBytesDoneCount);
            saea.SetBuffer(saea.Offset, userToken.messageBytes.Length - userToken.messageBytesDoneCount);
            if (!saea.AcceptSocket.SendAsync(saea))
            {
                monProcessSend(saea);
            }
        }

        private static void monProcessSend(SocketAsyncEventArgs saea)
        {
            var userToken = (DataHoldingUserToken)saea.UserToken;

            if (saea.SocketError == SocketError.Success)
            {
                userToken.messageBytesDoneCount += saea.BytesTransferred;

                if (userToken.messageBytesDoneCount == userToken.messageBytes.Length)
                {
                    // отправка завершена
                    userToken.reset();
                    userToken.socketGroup.waitWhileSendToMon.Set();
                }
                else
                {
                    monStartSend(saea);
                }
            }
            else
            {
                monSendCloseSocket(saea, userToken.socketGroup);
                userToken.socketGroup.waitWhileSendToMon.Set();
            }
        }

        private void monReceiveCloseSocket(SocketAsyncEventArgs saea, SocketGroup socketGroup)
        {
            try
            {
                if (saea.AcceptSocket.Connected)
                {
                    saea.AcceptSocket.Shutdown(SocketShutdown.Both);
                }
                saea.AcceptSocket.Close();

                if (socketGroup.blockReceiveSAEA.AcceptSocket.Connected)
                {
                    socketGroup.blockReceiveSAEA.AcceptSocket.Shutdown(SocketShutdown.Both);
                }
                socketGroup.blockReceiveSAEA.AcceptSocket.Close();
            }
            catch
            {
            }

        }

        private static void monSendCloseSocket(SocketAsyncEventArgs saea, SocketGroup socketGroup)
        {
            try
            {
                if (saea.AcceptSocket.Connected)
                {
                    saea.AcceptSocket.Shutdown(SocketShutdown.Both);
                }
                saea.AcceptSocket.Close();

                if (socketGroup.blockReceiveSAEA.AcceptSocket.Connected)
                {
                    socketGroup.blockReceiveSAEA.AcceptSocket.Shutdown(SocketShutdown.Both);
                }
                socketGroup.blockReceiveSAEA.AcceptSocket.Close();
            }
            catch
            {
            }
        }

        internal bool setSendSaea(SocketGroup socketGroup)
        {
            if (socketGroup.monSendSAEA == null)
            {
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

                socketGroup.monSendSAEA = createSaea(monSendEventHandler, BUFFER_SIZE);
                socketGroup.monSendSAEA.AcceptSocket = socket;
                ((DataHoldingUserToken)socketGroup.monSendSAEA.UserToken).socketGroup = socketGroup;

                socketGroup.monReceiveSAEA = createSaea(monReceiveEventHandler, BUFFER_SIZE);
                socketGroup.monReceiveSAEA.AcceptSocket = socket;
                ((DataHoldingUserToken)socketGroup.monReceiveSAEA.UserToken).socketGroup = socketGroup;

                monStartReceive(socketGroup.monReceiveSAEA);
            }
            return true;
        }

        internal void enqueueForSend(KeyValuePair<byte[], SocketAsyncEventArgs> keyValuePair)
        {
            monSendQueue.Enqueue(keyValuePair);
        }

        public override void startSend(SocketAsyncEventArgs saea, byte[] bytes)
        {
            monStartSend(saea, bytes);
        }
    }
}
