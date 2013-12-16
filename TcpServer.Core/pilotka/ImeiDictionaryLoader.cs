using log4net;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using TcpServer.Core.async.common;

namespace TcpServer.Core.pilotka
{
    public enum PilotkaState
    {
        Undefined, Started, Stoped
    }

    public class StateSended
    {
        public PilotkaState state = PilotkaState.Undefined;
        public bool sended = false;
    }

    public class ImeiDictionaryLoader
    {
        public static Dictionary<string, StateSended> loadDictionary(PilotkaSettings settings)
        {
            ILog log = LogManager.GetLogger(settings.LoggerName);
            Dictionary<string, StateSended> imeiDictionary = new Dictionary<string, StateSended>();

            var imeiSet = ImeiListLoader.loadImeis(log, settings.ImeiListFileName);
            foreach (string imei in imeiSet)
            {
                imeiDictionary.Add(imei, new StateSended());
            }
            return imeiDictionary;
        }
    }
}
