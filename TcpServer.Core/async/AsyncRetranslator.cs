using log4net;
using log4net.Config;
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net;
using System.Net.Sockets;
using System.Text;
using System.Threading;

namespace TcpServer.Core.async
{
    public class AsyncRetranslator
    {
        private const int BACKLOG = 500;
        private const int BUFFER_SIZE = 512;

        private static ILog log = LogManager.GetLogger(typeof(AsyncRetranslator));
        private static ILog packetLog = LogManager.GetLogger("packet");

        private Socket listenSocket;
        private string listenHost;
        private int listenPort;
        private string monHost;
        private int monPort;
        private IPEndPoint monEndPoint;

        private EventHandler<SocketAsyncEventArgs> blockAcceptEventHandler;
        private EventHandler<SocketAsyncEventArgs> blockReceiveEventHandler;
        private EventHandler<SocketAsyncEventArgs> blockSendEventHandler;
        private EventHandler<SocketAsyncEventArgs> monReceiveEventHandler;
        private EventHandler<SocketAsyncEventArgs> monSendEventHandler;

        private volatile bool shutdown = false;

        private ConcurrentQueue<KeyValuePair<byte[], SocketAsyncEventArgs>> monSendQueue = new ConcurrentQueue<KeyValuePair<byte[], SocketAsyncEventArgs>>();
        private ConcurrentQueue<KeyValuePair<byte[], SocketAsyncEventArgs>> blockSendQueue = new ConcurrentQueue<KeyValuePair<byte[], SocketAsyncEventArgs>>();

        private List<SendToMonitoringWorker> listSendToMonWorkers = new List<SendToMonitoringWorker>();
        private List<SendToBlockWorker> listSendToBlockWorkers = new List<SendToBlockWorker>();

        private readonly int cntSendToMonWorkers = 15;
        private readonly int cntSendToBlockWorkers = 15;

        public AsyncRetranslator(string listenHost, int listenPort, string monHost, int monPort)
        {
            string appPath = Path.GetDirectoryName(System.Reflection.Assembly.GetEntryAssembly().Location);
            string log4netConfigPath = Path.Combine(appPath, "log4net.config");
            FileInfo fi = new FileInfo(log4netConfigPath);
            XmlConfigurator.ConfigureAndWatch(fi);
            log = LogManager.GetLogger(typeof(AsyncRetranslator));
            packetLog = LogManager.GetLogger("packet");

            this.listenHost = listenHost;
            this.listenPort = listenPort;

            this.monHost = monHost;
            this.monPort = monPort;
        }

        private void init()
        {
            var monIPAddress = IPAddress.Parse(monHost);
            monEndPoint = new IPEndPoint(monIPAddress, monPort);

            blockAcceptEventHandler = new EventHandler<SocketAsyncEventArgs>(blockAcceptEvent);
            blockReceiveEventHandler = new EventHandler<SocketAsyncEventArgs>(blockReceiveEvent);
            blockSendEventHandler = new EventHandler<SocketAsyncEventArgs>(blockSendEvent);
            monSendEventHandler = new EventHandler<SocketAsyncEventArgs>(monSendEvent);
            monReceiveEventHandler = new EventHandler<SocketAsyncEventArgs>(monReceiveEvent);

            for (int i = 0; i < cntSendToMonWorkers; i++)
            {
                SendToMonitoringWorker w = new SendToMonitoringWorker(monSendQueue);
                w.start();
                listSendToMonWorkers.Add(w);
            }

            for (int i = 0; i < cntSendToBlockWorkers; i++)
            {
                SendToBlockWorker w = new SendToBlockWorker(blockSendQueue);
                w.start();
                listSendToBlockWorkers.Add(w);
            }
        }

        private SocketAsyncEventArgs createSaea(EventHandler<SocketAsyncEventArgs> eventHandler, int bufferSize)
        {
            var saea = new SocketAsyncEventArgs();
            saea.Completed += eventHandler;
            saea.SetBuffer(new byte[bufferSize], 0, bufferSize);
            saea.UserToken = new DataHoldingUserToken();
            return saea;
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

        private void monReceiveEvent(object sender, SocketAsyncEventArgs saea)
        {
            monProcessReceive(saea);
        }

        private void monSendEvent(object sender, SocketAsyncEventArgs saea)
        {
            monProcessSend(saea);
        }

        public void start()
        {
            log.Info("Starting retranslator...");

            init();

            var listenIPAddress = IPAddress.Parse(listenHost);
            var localEndPoint = new IPEndPoint(listenIPAddress, listenPort);

            listenSocket = new Socket(localEndPoint.AddressFamily, SocketType.Stream, ProtocolType.Tcp);
            listenSocket.Bind(localEndPoint);
            listenSocket.Listen(BACKLOG);

            log.Info("Retranslator started.");

            blockStartAccept();
        }

        public void stop()
        {
            shutdown = true;

            log.Info("Stoping retranslator...");

            if (listenSocket.Connected)
            {
                listenSocket.Shutdown(SocketShutdown.Both);
            }
            listenSocket.Close();

            foreach (SendToBlockWorker w in listSendToBlockWorkers)
            {
                w.stop();
            }

            foreach (SendToMonitoringWorker w in listSendToMonWorkers)
            {
                w.stop();
            }

            Thread.Sleep(4000);

            log.Info("Retranslator stoped.");
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
                if (userToken.prefixBytesDoneCount < ReceivePrefixHandler.RECEIVE_PREFIX_LENGTH)
                {
                    bytesToProcess = ReceivePrefixHandler.handlePrefix(saea, userToken, bytesToProcess);

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
                }

                bytesToProcess = ReceiveMessageHandler.handleMessage(saea, userToken, bytesToProcess, ReceivePacketProcessor.processMessageFromBlock);
            }

            blockStartReceive(saea);
        }

        public static void blockStartSend(SocketAsyncEventArgs saea, byte[] bytes)
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

        private static void blockStartSend(SocketAsyncEventArgs saea)
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

        private static void blockProcessSend(SocketAsyncEventArgs saea)
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
                if (socketGroup.blockSendSAEA == null)
                {
                    socketGroup.blockSendSAEA = createSaea(blockSendEventHandler, BUFFER_SIZE);
                    socketGroup.blockSendSAEA.AcceptSocket = socketGroup.blockReceiveSAEA.AcceptSocket;
                    ((DataHoldingUserToken)socketGroup.blockSendSAEA.UserToken).socketGroup = socketGroup;
                }

                byte[] bytes = new byte[saea.BytesTransferred];
                Buffer.BlockCopy(saea.Buffer, saea.Offset, bytes, 0, saea.BytesTransferred);

                blockSendQueue.Enqueue(new KeyValuePair<byte[], SocketAsyncEventArgs>(bytes, socketGroup.blockSendSAEA));
                //lock (blockSendQueue)
                //{
                //    Monitor.PulseAll(blockSendQueue);
                //}

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

        private static void monSendCloseSocket(SocketAsyncEventArgs saea, SocketGroup socketGroup)
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

        private static void blockSendCloseSocket(SocketAsyncEventArgs saea, SocketGroup socketGroup)
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

        private void blockReceiveCloseSocket(SocketAsyncEventArgs saea, SocketGroup socketGroup)
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
    }
}
