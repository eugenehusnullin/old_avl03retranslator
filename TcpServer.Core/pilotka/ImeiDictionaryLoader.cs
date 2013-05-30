using log4net;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace TcpServer.Core.pilotka
{
    public enum EngineState
    {
        Undefined, Started, Stoped
    }

    public class StateSended
    {
        public EngineState state = EngineState.Undefined;
        public bool sended = false;
    }

    public class ImeiDictionaryLoader
    {
        public static Dictionary<string, StateSended> loadDictionary(PilotkaSettings settings)
        {
            ILog log = LogManager.GetLogger(settings.LoggerName);
            Dictionary<string, StateSended> imeiDictionary = new Dictionary<string, StateSended>();

            string imeiListFileName = settings.ImeiListFileName;

            try
            {
                if (!File.Exists(imeiListFileName))
                {
                    string servicePath = Path.GetDirectoryName(System.Reflection.Assembly.GetEntryAssembly().Location);
                    imeiListFileName = Path.Combine(servicePath, settings.ImeiListFileName);

                    if (!File.Exists(imeiListFileName))
                    {
                        log.ErrorFormat("Imei list file {0} not exists.", settings.ImeiListFileName);
                        return imeiDictionary;
                    }
                }
            }
            catch (Exception e)
            {
                log.Error(String.Format("Exception on loading Imei list file {0}.", settings.ImeiListFileName), e);
                return imeiDictionary;
            }
            
            using (StreamReader reader = new StreamReader(File.OpenRead(imeiListFileName)))
            {
                while(!reader.EndOfStream)
                {
                    string imei = reader.ReadLine();
                    if(!string.IsNullOrEmpty(imei))
                    {
                        imeiDictionary.Add(imei, new StateSended());
                    }
                }
            }

            return imeiDictionary;
        }
    }
}
