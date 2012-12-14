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
        private const int INIT_ACCEPT_BLOCK_POOL_SIZE = 50;
        private const int BACKLOG = 500;

        private const int INIT_BLOCK_POOL_SIZE = 1000;
        private const int RECEIVE_BUFFER_SIZE = 512;
        private const int SEND_BUFFER_SIZE = 512;
        
        private const int RECEIVE_PREFIX_LENGTH = 4;

        private const int ATTEMPT_CONNECT_TO_MONITORING = 3;
        private const int TIMEOUT_BETWEEN_ATTEMPT_CONNECT_TO_MONITORING = 7000;

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

        private ConcurrentQueue<KeyValuePair<byte[], SocketAsyncEventArgs>> monSendQueue = new ConcurrentQueue<KeyValuePair<byte[],SocketAsyncEventArgs>>();
        private ConcurrentQueue<KeyValuePair<byte[], SocketAsyncEventArgs>> blockSendQueue = new ConcurrentQueue<KeyValuePair<byte[],SocketAsyncEventArgs>>();

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

        private SocketAsyncEventArgs createSAEABlockAccept()
        {
            var saea = new SocketAsyncEventArgs();
            saea.Completed += blockAcceptEventHandler;

            return saea;
        }

        private SocketAsyncEventArgs createSAEABlockReceive()
        {
            var saea = new SocketAsyncEventArgs();
            saea.Completed += blockReceiveEvent;
            saea.SetBuffer(new byte[RECEIVE_BUFFER_SIZE], 0, RECEIVE_BUFFER_SIZE);

            var userToken = new DataHoldingUserToken();
            userToken.socketGroup = new SocketGroup();
            userToken.socketGroup.blockReceiveSAEA = saea;
            saea.UserToken = userToken;

            return saea;
        }

        private SocketAsyncEventArgs createSAEABlockSend()
        {
            var saea = new SocketAsyncEventArgs();
            saea.Completed += blockSendEvent;
            saea.SetBuffer(new byte[SEND_BUFFER_SIZE], 0, SEND_BUFFER_SIZE);
            saea.UserToken = new DataHoldingUserToken();
            return saea;
        }

        private SocketAsyncEventArgs createSAEAMonSend()
        {
            var saea = new SocketAsyncEventArgs();
            saea.Completed += monSendEvent;
            saea.SetBuffer(new byte[SEND_BUFFER_SIZE], 0, SEND_BUFFER_SIZE);
            saea.UserToken = new DataHoldingUserToken();
            return saea;
        }

        private SocketAsyncEventArgs createSAEAMonReceive()
        {
            var saea = new SocketAsyncEventArgs();
            saea.Completed += monReceiveEvent;
            saea.SetBuffer(new byte[SEND_BUFFER_SIZE], 0, RECEIVE_BUFFER_SIZE);
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
            
            log.Info("Retranslator stoped.");
        }

        private void blockStartAccept()
        {
            SocketAsyncEventArgs saea = createSAEABlockAccept();

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
                    SocketAsyncEventArgs rSaea = createSAEABlockReceive();
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
            var userToken = (DataHoldingUserToken) saea.UserToken;

            if (saea.SocketError != SocketError.Success || saea.BytesTransferred == 0)
            {
                userToken.reset();
                blockReceiveCloseSocket(saea, userToken.socketGroup);
                return;
            }

            int bytesToProcess = saea.BytesTransferred;
            while (bytesToProcess > 0)
            {
                if (userToken.prefixBytesDoneCount < RECEIVE_PREFIX_LENGTH)
                {
                    bytesToProcess = handlePrefix(saea, userToken, bytesToProcess);

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

                bytesToProcess = handleMessage(saea, userToken, bytesToProcess);
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
                    socketGroup.blockSendSAEA = createSAEABlockSend();
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

        private int handlePrefix(SocketAsyncEventArgs rs, DataHoldingUserToken userToken, int bytesToProcess)
        {
            if (userToken.prefixBytesDoneCount == 0)
            {
                userToken.prefixBytes = new byte[RECEIVE_PREFIX_LENGTH];
            }

            int length = Math.Min(RECEIVE_PREFIX_LENGTH - userToken.prefixBytesDoneCount, bytesToProcess);

            Buffer.BlockCopy(rs.Buffer, rs.Offset
                + userToken.prefixBytesDoneCountThisOp + userToken.messageBytesDoneCountThisOp,
                userToken.prefixBytes, userToken.prefixBytesDoneCount, length);

            userToken.prefixBytesDoneCount += length;
            userToken.prefixBytesDoneCountThisOp += length;

            if (userToken.prefixBytesDoneCount == RECEIVE_PREFIX_LENGTH)
            {
                // заголовок готов, проверяем его, если он нормальный устанавливаем длину ожидаемого сообщения
                var prefix = Encoding.ASCII.GetString(userToken.prefixBytes);
                if (!prefix.StartsWith("$$"))
                {
                    log.WarnFormat("Someone sended us a bad packet with prefix={0} his IP={1}", prefix, ((IPEndPoint)rs.AcceptSocket.RemoteEndPoint).Address);
                    return -1;
                }

                try
                {
                    userToken.messageLength = Convert.ToInt32(prefix.Substring(2), 16) - 4;
                    if (userToken.messageLength <= 0)
                    {
                        return -2;
                    }
                }
                catch
                {
                    log.WarnFormat("Someone sended us a bad packet size prefix={0} his IP={1}", prefix, ((IPEndPoint)rs.AcceptSocket.RemoteEndPoint).Address);
                }
            }

            return bytesToProcess - length;
        }

        private int handleMessage(SocketAsyncEventArgs rs, DataHoldingUserToken userToken, int bytesToProcess)
        {
            if (userToken.messageBytesDoneCount == 0)
            {
                userToken.messageBytes = new byte[userToken.messageLength];
            }

            int length = Math.Min(userToken.messageLength - userToken.messageBytesDoneCount, bytesToProcess);

            Buffer.BlockCopy(rs.Buffer, rs.Offset +
                userToken.prefixBytesDoneCountThisOp + userToken.messageBytesDoneCountThisOp,
                userToken.messageBytes, userToken.messageBytesDoneCount, length);

            userToken.messageBytesDoneCount += length;
            userToken.messageBytesDoneCountThisOp += length;

            if (userToken.messageBytesDoneCount == userToken.messageLength)
            {
                //// сообщение готово
                var prefix = Encoding.ASCII.GetString(userToken.prefixBytes);
                var message = Encoding.ASCII.GetString(userToken.messageBytes);
                processMessageFromBlock(prefix, userToken.messageLength, message, userToken.socketGroup);

                userToken.reset();
            }

            return bytesToProcess - length;
        }

        private void processMessageFromBlock(string prefix, int lengthOfMessage, string message, SocketGroup socketGroup)
        {
            var receivedPacket = prefix + message;
            var basePacket = BasePacket.GetFromGlonass(receivedPacket);
            var gpsData = basePacket.ToPacketGps();
            
            packetLog.DebugFormat("src: {0}{1}dst: {2}", receivedPacket, Environment.NewLine, gpsData);

            var bytes = Encoding.ASCII.GetBytes(gpsData);

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
                    blockReceiveCloseSocket(socketGroup.blockReceiveSAEA, socketGroup);
                    return;
                }
                
                socketGroup.monSendSAEA = createSAEAMonSend();
                socketGroup.monSendSAEA.AcceptSocket = socket;
                ((DataHoldingUserToken)socketGroup.monSendSAEA.UserToken).socketGroup = socketGroup;

                socketGroup.monReceiveSAEA = createSAEAMonReceive();
                socketGroup.monReceiveSAEA.AcceptSocket = socket;
                ((DataHoldingUserToken)socketGroup.monReceiveSAEA.UserToken).socketGroup = socketGroup;
                
                monStartReceive(socketGroup.monReceiveSAEA);
            }
            
            monSendQueue.Enqueue(new KeyValuePair<byte[], SocketAsyncEventArgs>(bytes, socketGroup.monSendSAEA));
            //lock (monSendQueue)
            //{
            //    Monitor.PulseAll(monSendQueue);
            //}
        }
    }
}
