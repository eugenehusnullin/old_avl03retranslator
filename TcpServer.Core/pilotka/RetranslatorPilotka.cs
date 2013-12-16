using log4net;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using TcpServer.Core.Mintrans;

namespace TcpServer.Core.pilotka
{
    public class RetranslatorPilotka
    {
        private static int bitIndex = 6;
        private PilotkaSettings settings;
        private ILog log;
        private Dictionary<string, StateSended> imeiDictionary;
        private ObjectPool<WebRequestSender> webRequestSenderPool;

        public RetranslatorPilotka()
        {
            settings = new PilotkaSettings();

            if (settings.Enabled)
            {
                log = LogManager.GetLogger(settings.LoggerName);
                imeiDictionary = ImeiDictionaryLoader.loadDictionary(settings);
                webRequestSenderPool = new ObjectPool<WebRequestSender>(20, () => new WebRequestSender(settings));
            }
        }

        public async void retranslate(BasePacket packet)
        {
            try
            {
                if (settings.Enabled && imeiDictionary.ContainsKey(packet.IMEI))
                {
                    StateSended stateSended = imeiDictionary[packet.IMEI];

                    PilotkaState newState = packet.Status[bitIndex] == '0' ? PilotkaState.Stoped : PilotkaState.Started;

                    if (!stateSended.sended || stateSended.state != newState)
                    {
                        var webRequestSender = webRequestSenderPool.GetFromPool();
                        try
                        {
                            stateSended.state = newState;
                            stateSended.sended = await webRequestSender.send(packet.IMEI, newState, packet.ValidNavigDateTime);

                            if (stateSended.sended)
                            {
                                log.InfoFormat("Успешно: IMEI={0}, State={1}", packet.IMEI, packet.Status[bitIndex]);
                            }
                            else
                            {
                                log.WarnFormat("Провально: IMEI={0}, State={1}", packet.IMEI, packet.Status[bitIndex]);
                            }
                        }
                        finally
                        {
                            webRequestSenderPool.ReturnToPool(webRequestSender);
                        }
                    }
                }
            }
            catch (Exception e)
            {
                log.Error("RetranslatorPilotka.retranslate: " + e.ToString());
            }
        }

    }
}
