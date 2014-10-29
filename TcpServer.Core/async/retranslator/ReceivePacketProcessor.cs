using log4net;
using System;
using System.IO;
using System.Reflection;
using System.Text;
using TcpServer.Core.Mintrans;
using TcpServer.Core.pilotka;
using TcpServer.Core.Properties;

namespace TcpServer.Core.async.retranslator
{
    class ReceivePacketProcessor
    {
        private static ILog packetLog;
        private static ILog log;

        private RetranslatorTelemaxima retranslatorTelemaxima = null;
        private UnifiedProtocolSink mintransMoscowCitySink;
        private UnifiedProtocolSink mintransMoscowRegionSink;
        private RetranslatorPilotka retranslatorPilotka;

        private bool telemaximaEnabled = Settings.Default.Telemaxima_Enabled;

        public ReceivePacketProcessor()
        {
            packetLog = LogManager.GetLogger("packet");
            log = LogManager.GetLogger(typeof(ReceivePacketProcessor));

            if (telemaximaEnabled)
            {
                retranslatorTelemaxima = new RetranslatorTelemaxima();
            }

            this.mintransMoscowCitySink = UnifiedProtocolSink.GetInstance(new MintransMoscowCitySettings());
            this.mintransMoscowRegionSink = UnifiedProtocolSink.GetInstance(new MintransMoscowRegionSettings());
            this.retranslatorPilotka = new RetranslatorPilotka();
        }

        public void start()
        {
            if (telemaximaEnabled)
            {
                retranslatorTelemaxima.start();
            }

            this.mintransMoscowCitySink.start();
            this.mintransMoscowRegionSink.start();
        }

        public void stop()
        {
            if (telemaximaEnabled)
            {
                retranslatorTelemaxima.stop();
            }

            this.mintransMoscowCitySink.stop();
            this.mintransMoscowRegionSink.stop();
        }

        public byte[] processMessage(byte[] message, out string imei)
        {
            string receivedData = string.Empty;
            imei = null;
            try
            {
                receivedData = Encoding.ASCII.GetString(message);
                if (receivedData.StartsWith("$$"))
                {
                    var basePacket = BasePacket.GetFromGlonass(receivedData);
                    imei = basePacket.IMEI;

                    if (basePacket.State.Equals('V'))
                    {
                        basePacket.Latitude = 0.0d;
                        basePacket.Longitude = 0.0d;
                    }

                    this.mintransMoscowCitySink.SendLocationAndState(basePacket);
                    this.mintransMoscowRegionSink.SendLocationAndState(basePacket);
                    this.retranslatorPilotka.retranslate(basePacket);

                    if (telemaximaEnabled)
                    {
                        retranslatorTelemaxima.checkAndRetranslate(basePacket);
                    }

                    var gpsData = basePacket.ToPacketGps();

                    packetLog.DebugFormat("src: {0}{1}dst: {2}", receivedData, Environment.NewLine, gpsData);

                    return Encoding.ASCII.GetBytes(gpsData);
                }
                else
                {
                    return message;
                }
            }
            catch (ReflectionTypeLoadException ex)
            {
                StringBuilder sb = new StringBuilder();
                foreach (Exception exSub in ex.LoaderExceptions)
                {
                    sb.AppendLine(exSub.Message);
                    if (exSub is FileNotFoundException)
                    {
                        FileNotFoundException exFileNotFound = exSub as FileNotFoundException;
                        if (!string.IsNullOrEmpty(exFileNotFound.FusionLog))
                        {
                            sb.AppendLine("Fusion Log:");
                            sb.AppendLine(exFileNotFound.FusionLog);
                        }
                    }
                    sb.AppendLine();
                }
                string errorMessage = sb.ToString();
                //Display or log the error based on your application.
                log.Error("DLL = " + errorMessage, ex);
                return null;
            }
            catch (Exception e)
            {
                log.Error(String.Format("ProcessMessage packet={0}", receivedData), e);
                return null;
            }
        }
    }
}
