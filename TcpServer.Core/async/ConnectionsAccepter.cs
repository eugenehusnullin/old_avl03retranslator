using log4net;
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Net.Sockets;
using System.Text;
using System.Threading;

namespace TcpServer.Core.async
{
    public class ConnectionsAccepter
    {
        private const int INIT_ACCEPT_BLOCK_POOL_SIZE = 50;        
        private const int BACKLOG = 500;

        private const int INIT_BLOCK_POOL_SIZE = 1000;
        private const int RECEIVE_BUFFER_SIZE = 512;
        private const int SEND_BUFFER_SIZE = 512;
        
        private const int RECEIVE_PREFIX_LENGTH = 4;
        private const int SEND_PREFIX_LENGTH = 0;

        private Socket listenSocket;
        private string listenHost;
        private int listenPort;
        private string monHost;
        private int monPort;
        private IPEndPoint monEndPoint;

        private ConcurrentStack<SocketAsyncEventArgs> blockAcceptPool;
        private ConcurrentStack<SocketAsyncEventArgs> blockReceivePool;
        private ConcurrentStack<SocketAsyncEventArgs> blockSendPool;
        private ConcurrentStack<SocketAsyncEventArgs> monReceivePool;
        private ConcurrentStack<SocketAsyncEventArgs> monSendPool;

        private BufferManager blockReceiveBufferManager;
        private BufferManager blockSendBufferManager;
        private BufferManager monReceiveBufferManager;
        private BufferManager monSendBufferManager;

        private EventHandler<SocketAsyncEventArgs> blockAcceptEventHandler;
        private EventHandler<SocketAsyncEventArgs> blockReceiveEventHandler;
        private EventHandler<SocketAsyncEventArgs> blockSendEventHandler;        
        private EventHandler<SocketAsyncEventArgs> monReceiveEventHandler;
        private EventHandler<SocketAsyncEventArgs> monSendEventHandler;


        public ConnectionsAccepter(string listenHost, int listenPort, string monHost, int monPort)
        {
            this.listenHost = listenHost;
            this.listenPort = listenPort;

            this.monHost = monHost;
            this.monPort = monPort;

            LogManager.GetLogger(
        }

        private void init()
        {
            blockAcceptEventHandler = new EventHandler<SocketAsyncEventArgs>(blockAcceptEvent);
            blockAcceptPool = new ConcurrentStack<SocketAsyncEventArgs>();
            for (int i = 0; i < INIT_ACCEPT_BLOCK_POOL_SIZE; i++)
            {
                blockAcceptPool.Push(createSAEABlockAccept());
            }

            blockReceiveEventHandler = new EventHandler<SocketAsyncEventArgs>(blockReceiveEvent);
            blockReceiveBufferManager = new BufferManager(INIT_BLOCK_POOL_SIZE, RECEIVE_BUFFER_SIZE);
            blockReceivePool = new ConcurrentStack<SocketAsyncEventArgs>();
            for (int i = 0; i < INIT_BLOCK_POOL_SIZE; i++)
            {
                blockReceivePool.Push(createSAEABlockReceive());
            }

            blockSendEventHandler = new EventHandler<SocketAsyncEventArgs>(blockSendEvent);
            blockSendBufferManager = new BufferManager(INIT_BLOCK_POOL_SIZE, SEND_BUFFER_SIZE);
            blockSendPool = new ConcurrentStack<SocketAsyncEventArgs>();
            for (int i = 0; i < INIT_BLOCK_POOL_SIZE; i++)
            {
                blockSendPool.Push(createSAEABlockSend());
            }

            var monIPAddress = IPAddress.Parse(monHost);
            monEndPoint = new IPEndPoint(monIPAddress, monPort);

            monSendEventHandler = new EventHandler<SocketAsyncEventArgs>(monSendEvent);
            monSendBufferManager = new BufferManager(INIT_BLOCK_POOL_SIZE, SEND_BUFFER_SIZE);
            monSendPool = new ConcurrentStack<SocketAsyncEventArgs>();
            for (int i = 0; i < INIT_BLOCK_POOL_SIZE; i++)
            {
                monSendPool.Push(createSAEAMonSend());
            }

            monReceiveEventHandler = new EventHandler<SocketAsyncEventArgs>(monReceiveEvent);
            monReceiveBufferManager = new BufferManager(INIT_BLOCK_POOL_SIZE, RECEIVE_BUFFER_SIZE);
            monReceivePool = new ConcurrentStack<SocketAsyncEventArgs>();
            for (int i = 0; i < INIT_BLOCK_POOL_SIZE; i++)
            {
                monReceivePool.Push(createSAEAMonReceive());
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
            blockReceiveBufferManager.setBuffer(saea);

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
            blockSendBufferManager.setBuffer(saea);
            saea.UserToken = new DataHoldingUserToken();
            return saea;
        }

        private SocketAsyncEventArgs createSAEAMonSend()
        {
            var saea = new SocketAsyncEventArgs();
            saea.Completed += monSendEvent;
            monSendBufferManager.setBuffer(saea);
            saea.UserToken = new DataHoldingUserToken();
            return saea;
        }

        private SocketAsyncEventArgs createSAEAMonReceive()
        {
            var saea = new SocketAsyncEventArgs();
            saea.Completed += monReceiveEvent;
            monReceiveBufferManager.setBuffer(saea);
            saea.UserToken = new DataHoldingUserToken();
            return saea;
        }

        private void blockAcceptEvent(object sender, SocketAsyncEventArgs e)
        {
            blockProcessAccept(e);
        }

        private void blockReceiveEvent(object sender, SocketAsyncEventArgs e)
        {
            blockProcessReceive(e);
        }

        private void blockSendEvent(object sender, SocketAsyncEventArgs e)
        {
            blockProcessSend(e);            
        }

        private void monReceiveEvent(object sender, SocketAsyncEventArgs e)
        {
            monProcessReceive(e);
        }

        private void monSendEvent(object sender, SocketAsyncEventArgs e)
        {
            monProcessSend(e);
        }

        public void start()
        {
            init();

            var listenIPAddress = IPAddress.Parse(listenHost);
            var localEndPoint = new IPEndPoint(listenIPAddress, listenPort);

            listenSocket = new Socket(localEndPoint.AddressFamily, SocketType.Stream, ProtocolType.Tcp);
            listenSocket.Bind(localEndPoint);
            listenSocket.Listen(BACKLOG);

            blockStartAccept();
        }

        public void stop()
        {
            if (listenSocket.Connected)
            {
                listenSocket.Shutdown(SocketShutdown.Both);
            }
            listenSocket.Close();
        }

        private void blockStartAccept()
        {
            SocketAsyncEventArgs s;

            if (!blockAcceptPool.TryPop(out s))
            {
                s = createSAEABlockAccept();
            }

            if (!listenSocket.AcceptAsync(s))
            {
                blockProcessAccept(s);
            }
        }

        private void blockProcessAccept(SocketAsyncEventArgs e)
        {
            blockStartAccept();

            if (e.SocketError == SocketError.Success)
            {
                SocketAsyncEventArgs rSaea;

                if (!blockReceivePool.TryPop(out rSaea))
                {
                    rSaea = createSAEABlockReceive();
                }

                rSaea.AcceptSocket = e.AcceptSocket;
                e.AcceptSocket = null;

                blockStartReceive(rSaea);
            }
            else
            {
                e.AcceptSocket.Close();
            }
            blockAcceptPool.Push(e);
        }

        private void blockStartReceive(SocketAsyncEventArgs e)
        {
            var userToken = (DataHoldingUserToken)e.UserToken;            
            userToken.resetVariableForNewRequest();

            if (!e.AcceptSocket.ReceiveAsync(e))
            {
                blockProcessReceive(e);
            }
        }

        private void blockProcessReceive(SocketAsyncEventArgs rs)
        {            
            var userToken = (DataHoldingUserToken) rs.UserToken;

            if (rs.SocketError != SocketError.Success || rs.BytesTransferred == 0)
            {
                userToken.reset();
                closeBlockReceiveSocket(rs);
                return;
            }

            int bytesToProcess = rs.BytesTransferred;
            while (bytesToProcess > 0)
            {
                if (userToken.prefixBytesDoneCount < RECEIVE_PREFIX_LENGTH)
                {
                    bytesToProcess = handlePrefix(rs, userToken, bytesToProcess);

                    if (bytesToProcess < 0)
                    {
                        userToken.reset();
                        closeBlockReceiveSocket(rs);
                        return;
                    }
                    else if (bytesToProcess == 0)
                    {
                        blockStartReceive(rs);
                        return;
                    }
                }

                bytesToProcess = handleMessage(rs, userToken, bytesToProcess);
            }

            blockStartReceive(rs);
        }

        private void blockStartSend(SocketAsyncEventArgs e, byte[] bytes)
        {
            var userToken = (DataHoldingUserToken)e.UserToken;

            userToken.reset();
            userToken.messageBytes = bytes;
            blockStartSend(e);
        }

        private void blockStartSend(SocketAsyncEventArgs e)
        {
            var userToken = (DataHoldingUserToken)e.UserToken;
            var socketGroup = userToken.socketGroup;

            Buffer.BlockCopy(userToken.messageBytes, userToken.messageBytesDoneCount, e.Buffer,
                    e.Offset, userToken.messageBytes.Length - userToken.messageBytesDoneCount);
            e.SetBuffer(e.Offset, userToken.messageBytes.Length - userToken.messageBytesDoneCount);
            if (!e.AcceptSocket.SendAsync(e))
            {
                blockProcessSend(e);
            }
        }
        
        private void blockProcessSend(SocketAsyncEventArgs e)
        {
            if (e.SocketError == SocketError.Success)
            {
                var userToken = (DataHoldingUserToken)e.UserToken;
                userToken.messageBytesDoneCount += e.BytesTransferred;

                if (userToken.messageBytesDoneCount == userToken.messageBytes.Length)
                {
                    // отправка завершена
                    userToken.reset();
                    userToken.socketGroup.waitWhileSendToBlock.Set();
                }
                else
                {
                    blockStartSend(e);
                }
            }
            else
            {
                // TODO: закрыть сокеты к мониторингу и блоку
                // не забыть за блокировку, которая ожидает окончания текущей отправки
            }
        }

        private void monStartReceive(SocketAsyncEventArgs e)
        {
            var userToken = (DataHoldingUserToken)e.UserToken;
            userToken.resetVariableForNewRequest();

            if (!e.AcceptSocket.ReceiveAsync(e))
            {
                monProcessReceive(e);
            }
        }

        private void monProcessReceive(SocketAsyncEventArgs e)
        {
            var userToken = (DataHoldingUserToken)e.UserToken;
            var socketGroup = userToken.socketGroup;

            if (e.SocketError != SocketError.Success || e.BytesTransferred == 0)
            {
                // TODO: закрыть сокеты
            }
            else
            {
                if (socketGroup.blockSendSAEA == null)
                {
                    if (!blockSendPool.TryPop(out socketGroup.blockSendSAEA))
                    {
                        socketGroup.blockSendSAEA = createSAEABlockSend();
                    }
                    socketGroup.blockSendSAEA.AcceptSocket = socketGroup.blockReceiveSAEA.AcceptSocket;
                    ((DataHoldingUserToken)socketGroup.blockSendSAEA.UserToken).socketGroup = socketGroup;
                }

                byte[] bytes = new byte[e.BytesTransferred];
                Buffer.BlockCopy(e.Buffer, e.Offset, bytes, 0, e.BytesTransferred);

                socketGroup.waitWhileSendToBlock.WaitOne();
                blockStartSend(socketGroup.blockSendSAEA, bytes);

                monStartReceive(e);
            }
        }

        private void monStartSend(SocketAsyncEventArgs e, byte[] bytes)
        {
            var userToken = (DataHoldingUserToken)e.UserToken;

            userToken.reset();
            userToken.messageBytes = bytes;
            monStartSend(e);
        }

        private void monStartSend(SocketAsyncEventArgs e)
        {
            var userToken = (DataHoldingUserToken)e.UserToken;
            var socketGroup = userToken.socketGroup;

            Buffer.BlockCopy(userToken.messageBytes, userToken.messageBytesDoneCount, e.Buffer,
                    e.Offset, userToken.messageBytes.Length - userToken.messageBytesDoneCount);
            e.SetBuffer(e.Offset, userToken.messageBytes.Length - userToken.messageBytesDoneCount);
            if (!e.AcceptSocket.SendAsync(e))
            {
                monProcessSend(e);
            }
        }

        private void monProcessSend(SocketAsyncEventArgs e)
        {
            if (e.SocketError == SocketError.Success)
            {
                var userToken = (DataHoldingUserToken)e.UserToken;
                userToken.messageBytesDoneCount += e.BytesTransferred;

                if (userToken.messageBytesDoneCount == userToken.messageBytes.Length)
                {
                    // отправка завершена
                    userToken.reset();
                    userToken.socketGroup.waitWhileSendToMon.Set();
                }
                else
                {
                    monStartSend(e);
                }
            }
            else
            {
                // TODO: закрыть сокеты к мониторингу и блоку
                // не забыть за блокировку, которая ожидает окончания текущей отправки
            }
        }

        private void closeBlockReceiveSocket(SocketAsyncEventArgs e)
        {
            try
            {
                e.AcceptSocket.Shutdown(SocketShutdown.Both);
            }
            catch (Exception)
            {
            }

            e.AcceptSocket.Close();
            blockReceivePool.Push(e);
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
                    return -1;
                }

                try
                {
                    userToken.messageLength = Convert.ToInt32(prefix.Substring(2), 16) - 4;
                    if (userToken.messageLength <= 0)
                    {
                        return -3;
                    }
                }
                catch
                {
                    return -2;
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
            var data = prefix + message;
            var basePacket = BasePacket.GetFromGlonass(data);
            var gpsData = basePacket.ToPacketGps();

            var bytes = Encoding.ASCII.GetBytes(gpsData);

            if (socketGroup.monSendSAEA == null)
            {
                Socket socket = new Socket(AddressFamily.InterNetwork, SocketType.Stream, ProtocolType.Tcp);
                socket.Connect(monEndPoint);
                
                if (!monSendPool.TryPop(out socketGroup.monSendSAEA))
                {
                    socketGroup.monSendSAEA = createSAEAMonSend();
                }
                socketGroup.monSendSAEA.AcceptSocket = socket;
                ((DataHoldingUserToken)socketGroup.monSendSAEA.UserToken).socketGroup = socketGroup;

                
                if (!monReceivePool.TryPop(out socketGroup.monReceiveSAEA))
                {
                    socketGroup.monReceiveSAEA = createSAEAMonReceive();
                }
                socketGroup.monReceiveSAEA.AcceptSocket = socket;
                ((DataHoldingUserToken)socketGroup.monReceiveSAEA.UserToken).socketGroup = socketGroup;
                
                monStartReceive(socketGroup.monReceiveSAEA);
            }

            socketGroup.waitWhileSendToMon.WaitOne();
            monStartSend(socketGroup.monSendSAEA, bytes);
        }
    }
}
