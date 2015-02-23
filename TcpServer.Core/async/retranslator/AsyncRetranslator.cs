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
using TcpServer.Core.Properties;

namespace TcpServer.Core.async.retranslator
{
    public enum Action { Send2Mon, Send2Block }

    public class AsyncRetranslator
    {
        private ILog log;
        private ILog commandLog;

        private BlocksAcceptor blocksAcceptor;
        private MonConnector monConnector;
        private MonConnector mon2Connector;
        private ReceivePacketProcessor receivePacketProcessor;


        private BaseConnector.MessageReceived messageReceivedFromBlockDelegate;
        private BaseConnector.MessageReceived messageReceivedFromMonDelegate;
        private BaseConnector.MessageReceived messageReceivedFromMon2Delegate;

        private BaseConnector.MessageSended messageSendedToMonDelegate;
        private BaseConnector.MessageSended messageSendedToMon2Delegate;
        private BaseConnector.MessageSended messageSendedToBlockDelegate;


        private BaseConnector.ReceiveFailed blockReceiveFailedDelegate;
        private BaseConnector.SendFailed blockSendFailedDelegate;
        private BaseConnector.ReceiveFailed monReceiveFailedDelegate;
        private BaseConnector.SendFailed monSendFailedDelegate;
        private BaseConnector.ReceiveFailed mon2ReceiveFailedDelegate;
        private BaseConnector.SendFailed mon2SendFailedDelegate;

        private BlocksAcceptor.ConnectionAccepted blockConnectionAcceptedDelegate;

        private HashSet<string> mon2Imeis;

        private string appPath;

        public AsyncRetranslator(string listenHost, int listenPort, string monHost, int monPort)
        {
            appPath = Path.GetDirectoryName(System.Reflection.Assembly.GetEntryAssembly().Location);
            string log4netConfigPath = Path.Combine(appPath, "log4net.config");
            FileInfo fi = new FileInfo(log4netConfigPath);
            XmlConfigurator.ConfigureAndWatch(fi);
            log = LogManager.GetLogger(typeof(AsyncRetranslator));
            commandLog = LogManager.GetLogger("command");

            messageReceivedFromBlockDelegate = messageReceivedFromBlock;
            messageReceivedFromMonDelegate = messageReceivedFromMon;
            messageReceivedFromMon2Delegate = messageReceivedFromMon2;

            messageSendedToMonDelegate = messageSendedToMon;
            messageSendedToMon2Delegate = messageSendedToMon2;
            messageSendedToBlockDelegate = messageSendedToBlock;

            blockReceiveFailedDelegate = blockReceiveFailed;
            blockSendFailedDelegate = blockSendFailed;
            monReceiveFailedDelegate = monReceiveFailed;
            monSendFailedDelegate = monSendFailed;
            mon2ReceiveFailedDelegate = mon2ReceiveFailed;
            mon2SendFailedDelegate = mon2SendFailed;

            blockConnectionAcceptedDelegate = blockConnectionAccepted;

            
            blocksAcceptor = new BlocksAcceptor(listenHost, listenPort, messageReceivedFromBlockDelegate, messageSendedToBlockDelegate,
                blockConnectionAcceptedDelegate, blockReceiveFailedDelegate, blockSendFailedDelegate);
            monConnector = new MonConnector(monHost, monPort, messageReceivedFromMonDelegate, messageSendedToMonDelegate,
                monReceiveFailedDelegate, monSendFailedDelegate, 3, 7000);
            receivePacketProcessor = new ReceivePacketProcessor(blocksAcceptor);

            if (Settings.Default.Mon2_Enabled)
            {
                // 1. load imeis
                if (!Settings.Default.Mon2_Allboards)
                {
                    mon2Imeis = ImeiListLoader.loadImeis(log, Settings.Default.Mon2_ImeiListFileName);
                }

                // 2. init mon2connector
                mon2Connector = new MonConnector(Settings.Default.Mon2_Host, Settings.Default.Mon2_Port,
                    messageReceivedFromMon2Delegate, messageSendedToMon2Delegate, mon2ReceiveFailedDelegate, mon2SendFailedDelegate);
            }
        }

        public void start()
        {
            log.Info("Starting retranslator...");
            receivePacketProcessor.start();
            blocksAcceptor.start();
            log.Info("Retranslator started.");
        }

        public void stop()
        {
            log.Info("Stoping retranslator...");
            blocksAcceptor.stop();
            receivePacketProcessor.stop();
            log.Info("Retranslator stoped.");
        }

        private void blockConnectionAccepted(SocketAsyncEventArgs saea)
        {
            var socketGroup = new SocketGroup();
            ((DataHoldingUserToken)saea.UserToken).socketGroup = socketGroup;
            socketGroup.blockReceiveSAEA = saea;
            blocksAcceptor.startReceive(saea);
        }

        private void connectMon(SocketGroup socketGroup)
        {            
            SocketAsyncEventArgs monReceive, monSend;
            if (monConnector.createConnection(out monReceive, out monSend))
            {
                ((DataHoldingUserToken)monReceive.UserToken).socketGroup = socketGroup;
                ((DataHoldingUserToken)monSend.UserToken).socketGroup = socketGroup;
                socketGroup.monReceiveSAEA = monReceive;
                socketGroup.monSendSAEA = monSend;
                monConnector.startReceive(monReceive);
            }
            else
            {
                socketGroup.monReceiveSAEA = null;
                socketGroup.monSendSAEA = null;
            }
        }

        private void connectMon2(SocketGroup socketGroup)
        {            
            SocketAsyncEventArgs mon2Receive, mon2Send;
            if (mon2Connector.createConnection(out mon2Receive, out mon2Send))
            {
                ((DataHoldingUserToken)mon2Receive.UserToken).socketGroup = socketGroup;
                ((DataHoldingUserToken)mon2Send.UserToken).socketGroup = socketGroup;
                socketGroup.mon2ReceiveSAEA = mon2Receive;
                socketGroup.mon2SendSAEA = mon2Send;
                mon2Connector.startReceive(mon2Receive);
            }
            else
            {
                socketGroup.mon2ReceiveSAEA = null;
                socketGroup.mon2SendSAEA = null;
            }
        }

        

        private void messageReceivedFromBlock(byte[] message, SocketAsyncEventArgs saea)
        {
            if (log.IsDebugEnabled)
            {
                var receivedData = Encoding.ASCII.GetString(message);
                log.Debug("--- messageReceivedFromBlock --- " + receivedData);
            }

            SocketGroup socketGroup = (saea.UserToken as DataHoldingUserToken).socketGroup;

            if (Settings.Default.PureRetranslate)
            {
                string filename = Path.Combine(appPath,"purelog"); //((IPEndPoint)saea.AcceptSocket.RemoteEndPoint).Address.ToString();
                var fs = new FileStream(filename, FileMode.Append);
                string messageHead = "^device:";
                var messageHeadBytes = Encoding.ASCII.GetBytes(messageHead);
                fs.Write(messageHeadBytes, 0, messageHeadBytes.Length);
                fs.Write(message, 0, message.Length);

                var lineEnd = "\r\n";
                var lineEndBytes = Encoding.ASCII.GetBytes(lineEnd);
                fs.Write(lineEndBytes, 0, lineEndBytes.Length);

                fs.Close();

                if (socketGroup.monSendSAEA == null)
                {
                    connectMon(socketGroup);
                }
                monConnector.startSend(socketGroup.monSendSAEA, message);
            }
            else
            {
                string imei;
                Action action;
                byte[] processedBytes = receivePacketProcessor.processMessage(message, out imei, socketGroup, out action);

                if (processedBytes == null)
                {
                    blocksAcceptor.closeSocket(socketGroup.blockReceiveSAEA);
                    if (socketGroup.monSendSAEA != null)
                    {
                        monConnector.closeSocket(socketGroup.monSendSAEA);
                    }
                    return;
                }

                if (action == Action.Send2Block)
                {
                    messageReceivedFromMon(processedBytes, saea);
                }
                else
                {

                    if (imei != null && socketGroup.IMEI == null)
                    {
                        socketGroup.IMEI = imei;
                    }

                    if (socketGroup.monSendSAEA == null)
                    {
                        connectMon(socketGroup);
                    }
                    monConnector.startSend(socketGroup.monSendSAEA, processedBytes);

                    // mon2
                    if (Settings.Default.Mon2_Enabled && (Settings.Default.Mon2_Allboards || mon2Imeis.Contains(imei)))
                    {
                        if (socketGroup.mon2SendSAEA == null)
                        {
                            connectMon2(socketGroup);
                        }

                        if (Settings.Default.Mon2_Format)
                        {
                            mon2Connector.startSend(socketGroup.mon2SendSAEA, processedBytes);
                        }
                        else
                        {
                            mon2Connector.startSend(socketGroup.mon2SendSAEA, message);
                        }
                    }
                }
            }
        }

        private void messageSendedToMon(SocketAsyncEventArgs saea, byte[] message)
        {
            if (log.IsDebugEnabled)
            {
                var receivedData = Encoding.ASCII.GetString(message);
                log.Debug("--- messageSendedToMon --- " + receivedData);
            }

            var userToken = (DataHoldingUserToken)saea.UserToken;
            blocksAcceptor.startReceive(userToken.socketGroup.blockReceiveSAEA);
        }

        private void messageSendedToMon2(SocketAsyncEventArgs saea, byte[] message)
        {
        }

        private void messageReceivedFromMon(byte[] message, SocketAsyncEventArgs saea)
        {
            if (log.IsDebugEnabled)
            {
                var receivedData = Encoding.ASCII.GetString(message);
                log.Debug("--- messageReceivedFromMon --- " + receivedData);
            }

            if (Settings.Default.PureRetranslate)
            {
                string filename = Path.Combine(appPath,"purelog"); //((IPEndPoint)saea.AcceptSocket.RemoteEndPoint).Address.ToString();
                var fs = new FileStream(filename, FileMode.Append);
                string messageHead = "^server:";
                var messageHeadBytes = Encoding.ASCII.GetBytes(messageHead);
                fs.Write(messageHeadBytes, 0, messageHeadBytes.Length);
                fs.Write(message, 0, message.Length);

                var lineEnd = "\r\n";
                var lineEndBytes = Encoding.ASCII.GetBytes(lineEnd);
                fs.Write(lineEndBytes, 0, lineEndBytes.Length);

                fs.Close();
            }
            else
            {
                string str = Encoding.ASCII.GetString(message);
                commandLog.Debug(str);
            }

            SocketGroup socketGroup = (saea.UserToken as DataHoldingUserToken).socketGroup;
            if (socketGroup.blockSendSAEA == null)
            {
                socketGroup.blockSendSAEA = blocksAcceptor.createSaeaForSend(socketGroup.blockReceiveSAEA.AcceptSocket);
                ((DataHoldingUserToken)socketGroup.blockSendSAEA.UserToken).socketGroup = socketGroup;
            }

            blocksAcceptor.startSend(socketGroup.blockSendSAEA, message);
        }

        private void messageReceivedFromMon2(byte[] message, SocketAsyncEventArgs saea)
        {
            // log command
            {
                string str = Encoding.ASCII.GetString(message);
                commandLog.Debug(str);
            }

            SocketGroup socketGroup = (saea.UserToken as DataHoldingUserToken).socketGroup;
            if (socketGroup.blockSendSAEA == null)
            {
                socketGroup.blockSendSAEA = blocksAcceptor.createSaeaForSend(socketGroup.blockReceiveSAEA.AcceptSocket);
                ((DataHoldingUserToken)socketGroup.blockSendSAEA.UserToken).socketGroup = socketGroup;
            }

            blocksAcceptor.startSend(socketGroup.blockSendSAEA, message);
        }

        private void messageSendedToBlock(SocketAsyncEventArgs saea, byte[] message)
        {
            if (log.IsDebugEnabled)
            {
                var receivedData = Encoding.ASCII.GetString(message);
                log.Debug("--- messageSendedToBlock --- " + receivedData);
            }

            var userToken = (DataHoldingUserToken)saea.UserToken;
            monConnector.startReceive(userToken.socketGroup.monReceiveSAEA);
        }

        private void blockReceiveFailed(SocketAsyncEventArgs saea)
        {
            var socketGroup = (saea.UserToken as DataHoldingUserToken).socketGroup;
            if (socketGroup.monSendSAEA != null)
            {
                monConnector.closeSocket(socketGroup.monSendSAEA);
                (socketGroup.monSendSAEA.UserToken as DataHoldingUserToken).socketGroup = null;
                socketGroup.monSendSAEA.Dispose();
            }
        }

        private void blockSendFailed(SocketAsyncEventArgs saea)
        {
            var socketGroup = (saea.UserToken as DataHoldingUserToken).socketGroup;
            if (socketGroup.monReceiveSAEA != null)
            {
                monConnector.closeSocket(socketGroup.monReceiveSAEA);
                (socketGroup.monReceiveSAEA.UserToken as DataHoldingUserToken).socketGroup = null;
                socketGroup.monReceiveSAEA.Dispose();
            }
        }

        private void monReceiveFailed(SocketAsyncEventArgs saea)
        {
            var socketGroup = (saea.UserToken as DataHoldingUserToken).socketGroup;
            if (socketGroup.blockSendSAEA != null)
            {
                blocksAcceptor.closeSocket(socketGroup.blockSendSAEA);
                (socketGroup.blockSendSAEA.UserToken as DataHoldingUserToken).socketGroup = null;
                socketGroup.blockSendSAEA.Dispose();
            }
        }

        private void mon2ReceiveFailed(SocketAsyncEventArgs saea)
        {
            var socketGroup = (saea.UserToken as DataHoldingUserToken).socketGroup;
            socketGroup.mon2ReceiveSAEA = null;
            socketGroup.mon2SendSAEA = null;
        }

        private void monSendFailed(SocketAsyncEventArgs saea)
        {
            var socketGroup = (saea.UserToken as DataHoldingUserToken).socketGroup;
            if (socketGroup.blockReceiveSAEA != null)
            {
                blocksAcceptor.closeSocket(socketGroup.blockReceiveSAEA);
                (socketGroup.blockReceiveSAEA.UserToken as DataHoldingUserToken).socketGroup = null;
                socketGroup.blockReceiveSAEA.Dispose();
            }
        }

        private void mon2SendFailed(SocketAsyncEventArgs saea)
        {
            var socketGroup = (saea.UserToken as DataHoldingUserToken).socketGroup;
            socketGroup.mon2ReceiveSAEA = null;
            socketGroup.mon2SendSAEA = null;
        }
    }
}
