using log4net;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Sockets;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace TcpServer.Core.async
{
    class ReceivePacketProcessor
    {
        private static int ATTEMPT_CONNECT_TO_MONITORING = 3;
        private static int TIMEOUT_BETWEEN_ATTEMPT_CONNECT_TO_MONITORING = 7000;
        private static int BUFFER_SIZE = 512;

        

        public static void processMessageFromBlock(string prefix, string message, SocketGroup socketGroup)
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

                socketGroup.monSendSAEA = createSaea(monSendEventHandler, BUFFER_SIZE);
                socketGroup.monSendSAEA.AcceptSocket = socket;
                ((DataHoldingUserToken)socketGroup.monSendSAEA.UserToken).socketGroup = socketGroup;

                socketGroup.monReceiveSAEA = createSaea(monReceiveEventHandler, BUFFER_SIZE);
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
