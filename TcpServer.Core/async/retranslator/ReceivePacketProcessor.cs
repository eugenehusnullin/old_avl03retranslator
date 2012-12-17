using log4net;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Sockets;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace TcpServer.Core.async.retranslator
{
    class ReceivePacketProcessor
    {
        private static ILog packetLog;
        private static ILog log;

        public ReceivePacketProcessor()
        {
            packetLog = LogManager.GetLogger("packet");
            log = LogManager.GetLogger(typeof(ReceivePacketProcessor));
        }

        public byte[] processMessage(byte[] message)
        {
            try
            {
                var receivedPacket = Encoding.ASCII.GetString(message);
                var basePacket = BasePacket.GetFromGlonass(receivedPacket);
                var gpsData = basePacket.ToPacketGps();

                packetLog.DebugFormat("src: {0}{1}dst: {2}", receivedPacket, Environment.NewLine, gpsData);

                return Encoding.ASCII.GetBytes(gpsData);
            }
            catch(Exception e)
            {
                log.Warn("processMessage", e);
                return null;
            }
        }
    }
}
