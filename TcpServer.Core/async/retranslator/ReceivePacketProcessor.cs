using log4net;
using System;
using System.Text;
using TcpServer.Core.Mintrans;

namespace TcpServer.Core.async.retranslator
{
    class ReceivePacketProcessor
    {
        private static ILog packetLog;
        private static ILog log;

        private RetranslatorTelemaxima retranslatorTelemaxima;
        private UnifiedProtocolSink mintransMoscowCitySink;
        private UnifiedProtocolSink mintransMoscowRegionSink;

        public ReceivePacketProcessor()
        {
            packetLog = LogManager.GetLogger("packet");
            log = LogManager.GetLogger(typeof(ReceivePacketProcessor));

            retranslatorTelemaxima = new RetranslatorTelemaxima();
            this.mintransMoscowCitySink = UnifiedProtocolSink.GetInstance(new MintransMoscowCitySettings());
            this.mintransMoscowRegionSink = UnifiedProtocolSink.GetInstance(new MintransMoscowRegionSettings());
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
                    this.mintransMoscowCitySink.SendLocationAndState(basePacket);
                    this.mintransMoscowRegionSink.SendLocationAndState(basePacket);

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
