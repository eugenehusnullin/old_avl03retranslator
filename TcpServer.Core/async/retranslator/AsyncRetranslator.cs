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
using TcpServer.Core.async.block;
using TcpServer.Core.async.common;
using TcpServer.Core.async.mon;

namespace TcpServer.Core.async.retranslator
{
    public class AsyncRetranslator
    {
        private ILog log;
        
        private BlocksAcceptor blocksAcceptor;
        private MonConnector monConnector;
        private ReceivePacketProcessor receivePacketProcessor;

        
        private BaseConnector.MessageReceived messageReceivedFromBlockDelegate;
        private BaseConnector.MessageReceived messageReceivedFromMonDelegate;

        private BaseConnector.MessageSended messageSendedToMonDelegate;
        private BaseConnector.MessageSended messageSendedToBlockDelegate;

        private BaseConnector.ReceiveFailed blockReceiveFailedDelegate;
        private BaseConnector.SendFailed blockSendFailedDelegate;
        private BaseConnector.ReceiveFailed monReceiveFailedDelegate;
        private BaseConnector.SendFailed monSendFailedDelegate;

        private BlocksAcceptor.ConnectionAccepted blockConnectionAcceptedDelegate;

        private volatile int connectionsToBlock = 0;
        private object syncCntToBlock = new object();

        public AsyncRetranslator(string listenHost, int listenPort, string monHost, int monPort)
        {
            string appPath = Path.GetDirectoryName(System.Reflection.Assembly.GetEntryAssembly().Location);
            string log4netConfigPath = Path.Combine(appPath, "log4net.config");
            FileInfo fi = new FileInfo(log4netConfigPath);
            XmlConfigurator.ConfigureAndWatch(fi);
            log = LogManager.GetLogger(typeof(AsyncRetranslator));

            messageReceivedFromBlockDelegate = messageReceivedFromBlock;
            messageReceivedFromMonDelegate = messageReceivedFromMon;

            messageSendedToMonDelegate = messageSendedToMon;
            messageSendedToBlockDelegate = messageSendedToBlock;

            blockReceiveFailedDelegate = blockReceiveFailed;
            blockSendFailedDelegate = blockSendFailed;
            monReceiveFailedDelegate = monReceiveFailed;
            monSendFailedDelegate = monSendFailed;

            blockConnectionAcceptedDelegate = blockConnectionAccepted;

            receivePacketProcessor = new ReceivePacketProcessor();
            blocksAcceptor = new BlocksAcceptor(listenHost, listenPort, messageReceivedFromBlockDelegate, messageSendedToBlockDelegate,
                blockConnectionAcceptedDelegate, blockReceiveFailedDelegate, blockSendFailedDelegate);
            monConnector = new MonConnector(monHost, monPort, messageReceivedFromMonDelegate, messageSendedToMonDelegate,
                monReceiveFailedDelegate, monSendFailedDelegate);
        }

        public void start()
        {
            log.Info("Starting retranslator...");
            blocksAcceptor.start();
            log.Info("Retranslator started.");
        }

        public void stop()
        {
            log.Info("Stoping retranslator...");
            blocksAcceptor.stop();
            log.Info("Retranslator stoped.");
        }

        private void blockConnectionAccepted(SocketAsyncEventArgs saea)
        {
            incrementCountConnectionsToBlock();
            var userToken = (DataHoldingUserToken)saea.UserToken;
            userToken.socketGroup = new SocketGroup();
            userToken.socketGroup.blockReceiveSAEA = saea;

            blocksAcceptor.startReceive(saea);
        }

        private void incrementCountConnectionsToBlock()
        {
            lock (syncCntToBlock)
            {
                connectionsToBlock++;
                log.DebugFormat("Block connections: {0}", connectionsToBlock);
            }
        }

        private void decrementCountConnectionsToBlock()
        {
            lock (syncCntToBlock)
            {
                connectionsToBlock--;
                log.DebugFormat("Block connections: {0}", connectionsToBlock);
            }
        }

        private void messageReceivedFromBlock(byte[] message, SocketAsyncEventArgs saea)
        {
            SocketGroup socketGroup = (saea.UserToken as DataHoldingUserToken).socketGroup;

            byte[] processedBytes = receivePacketProcessor.processMessage(message);

            if (processedBytes == null)
            {
                decrementCountConnectionsToBlock();
                blocksAcceptor.closeSocket(socketGroup.blockReceiveSAEA);
                return;
            }

            if (socketGroup.monSendSAEA == null)
            {
                SocketAsyncEventArgs monReceive, monSend;
                if (monConnector.createConnection(out monReceive, out monSend))
                {
                    socketGroup.monSendSAEA = monSend;
                    ((DataHoldingUserToken)socketGroup.monSendSAEA.UserToken).socketGroup = socketGroup;

                    socketGroup.monReceiveSAEA = monReceive;
                    ((DataHoldingUserToken)socketGroup.monReceiveSAEA.UserToken).socketGroup = socketGroup;

                    monConnector.startReceive(monReceive);
                }
                else
                {
                    decrementCountConnectionsToBlock();
                    blocksAcceptor.closeSocket(socketGroup.blockReceiveSAEA);
                    return;
                }
            }
            monConnector.startSend(socketGroup.monSendSAEA, processedBytes);
        }

        private void messageSendedToMon(SocketAsyncEventArgs saea)
        {
            var userToken = (DataHoldingUserToken)saea.UserToken;
            blocksAcceptor.startReceive(userToken.socketGroup.blockReceiveSAEA);
        }

        private void messageReceivedFromMon(byte[] message, SocketAsyncEventArgs saea)
        {
            SocketGroup socketGroup = (saea.UserToken as DataHoldingUserToken).socketGroup;
            if (socketGroup.blockSendSAEA == null)
            {
                socketGroup.blockSendSAEA = blocksAcceptor.createSaeaForSend(socketGroup.blockReceiveSAEA.AcceptSocket);
                ((DataHoldingUserToken)socketGroup.blockSendSAEA.UserToken).socketGroup = socketGroup;
            }
            
            blocksAcceptor.startSend(socketGroup.blockSendSAEA, message);
        }

        private void messageSendedToBlock(SocketAsyncEventArgs saea)
        {
            var userToken = (DataHoldingUserToken)saea.UserToken;
            monConnector.startReceive(userToken.socketGroup.monReceiveSAEA);
        }

        private void blockReceiveFailed(SocketAsyncEventArgs saea)
        {
            decrementCountConnectionsToBlock();
            var socketGroup = (saea.UserToken as DataHoldingUserToken).socketGroup;
            if (socketGroup.monReceiveSAEA != null)
            {
                monConnector.closeSocket(socketGroup.monReceiveSAEA);
            }
        }

        private void blockSendFailed(SocketAsyncEventArgs saea)
        {
            var socketGroup = (saea.UserToken as DataHoldingUserToken).socketGroup;
            if (socketGroup.monReceiveSAEA != null)
            {
                monConnector.closeSocket(socketGroup.monReceiveSAEA);
            }
        }

        private void monReceiveFailed(SocketAsyncEventArgs saea)
        {
            var socketGroup = (saea.UserToken as DataHoldingUserToken).socketGroup;
            if (socketGroup.blockReceiveSAEA != null)
            {
                blocksAcceptor.closeSocket(socketGroup.blockReceiveSAEA);
            }
        }

        private void monSendFailed(SocketAsyncEventArgs saea)
        {
            decrementCountConnectionsToBlock();
            var socketGroup = (saea.UserToken as DataHoldingUserToken).socketGroup;
            if (socketGroup.blockReceiveSAEA != null)
            {
                blocksAcceptor.closeSocket(socketGroup.blockReceiveSAEA);
            }
        }
    }
}
