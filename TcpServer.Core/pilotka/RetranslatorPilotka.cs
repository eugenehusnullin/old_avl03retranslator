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

                    EngineState newEngineState = packet.Status[5] == '0' ? EngineState.Stoped : EngineState.Started;

                    if (!stateSended.sended || stateSended.state != newEngineState)
                    {
                        var webRequestSender = webRequestSenderPool.GetFromPool();
                        try
                        {
                            stateSended.state = newEngineState;
                            stateSended.sended = await webRequestSender.send(packet.IMEI, newEngineState, DateTime.UtcNow);
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
                log.Error("RetranslatorPilotka: " + e.ToString());
            }
        }

    }
}
