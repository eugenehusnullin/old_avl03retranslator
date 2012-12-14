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
        private ILog log;
        
        private BlockConnector blockConnector;
        private MonConnector monConnector;
        private ReceivePacketProcessor receivePacketProcessor;

        public delegate void MessageReady(byte[] message, SocketGroup socketGroup);
        private MessageReady blockMessageReady;
        private MessageReady monMessageReady;

        public AsyncRetranslator(string listenHost, int listenPort, string monHost, int monPort)
        {
            string appPath = Path.GetDirectoryName(System.Reflection.Assembly.GetEntryAssembly().Location);
            string log4netConfigPath = Path.Combine(appPath, "log4net.config");
            FileInfo fi = new FileInfo(log4netConfigPath);
            XmlConfigurator.ConfigureAndWatch(fi);
            log = LogManager.GetLogger(typeof(AsyncRetranslator));

            blockMessageReady = blockMessageReadyFunc;
            monMessageReady = monMessageReadyFunc;

            receivePacketProcessor = new ReceivePacketProcessor();
            blockConnector = new BlockConnector(listenHost, listenPort, blockMessageReady);
            monConnector = new MonConnector(monHost, monPort, monMessageReady);
        }

        public void start()
        {
            log.Info("Starting retranslator...");
            monConnector.start();
            blockConnector.start();
            log.Info("Retranslator started.");
        }

        public void stop()
        {
            log.Info("Stoping retranslator...");
            blockConnector.stop();
            monConnector.stop();
            Thread.Sleep(2000);
            log.Info("Retranslator stoped.");
        }

        private void blockMessageReadyFunc(byte[] message, SocketGroup socketGroup)
        {
            byte[] gpsFormatBytes = receivePacketProcessor.processMessage(message);

            if (gpsFormatBytes != null)
            {
                if (!monConnector.setSendSaea(socketGroup))
                {
                    blockConnector.blockReceiveCloseSocket(socketGroup.blockReceiveSAEA, socketGroup);
                }
                monConnector.enqueueForSend(new KeyValuePair<byte[], SocketAsyncEventArgs>(gpsFormatBytes, socketGroup.monSendSAEA));
            }
            else
            {
                blockConnector.blockReceiveCloseSocket(socketGroup.blockReceiveSAEA, socketGroup);
            }
        }

        private void monMessageReadyFunc(byte[] message, SocketGroup socketGroup)
        {
            blockConnector.setSendSaea(socketGroup);
            blockConnector.enqueueForSend(new KeyValuePair<byte[], SocketAsyncEventArgs>(message, socketGroup.blockSendSAEA));
        }
    }
}
