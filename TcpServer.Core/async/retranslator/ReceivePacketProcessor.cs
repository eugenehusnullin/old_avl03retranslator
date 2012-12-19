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

        private RetranslatorTelemaxima retranslatorTelemaxima;

        public ReceivePacketProcessor()
        {
            packetLog = LogManager.GetLogger("packet");
            log = LogManager.GetLogger(typeof(ReceivePacketProcessor));

            retranslatorTelemaxima = new RetranslatorTelemaxima();
        }

        public void start()
        {
            retranslatorTelemaxima.start();
        }

        public void stop()
        {
            retranslatorTelemaxima.stop();
        }

        public byte[] processMessage(byte[] message)
        {
            string receivedData = string.Empty;
            try
            {
                receivedData = Encoding.ASCII.GetString(message);
                if (receivedData.StartsWith("$$"))
                {
                    var basePacket = BasePacket.GetFromGlonass(receivedData);

                    retranslatorTelemaxima.checkAndRetranslate(basePacket);

                    var gpsData = basePacket.ToPacketGps();

                    packetLog.DebugFormat("src: {0}{1}dst: {2}", receivedData, Environment.NewLine, gpsData);

                    return Encoding.ASCII.GetBytes(gpsData);
                }
                else
                {
                    return message;
                }
            }
            catch(Exception e)
            {
                log.Error(String.Format("ProcessMessage packet={0}", receivedData), e);
                return null;
            }
        }
    }
}
