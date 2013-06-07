using log4net;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace TcpServer.Core.async.common
{
    public class ImeiListLoader
    {
        public static HashSet<string> loadImeis(ILog log, string imeiListFileNameOriginal)
        {
            HashSet<string> imeiSet = new HashSet<string>();

            string imeiListFileName = imeiListFileNameOriginal;

            try
            {
                if (!File.Exists(imeiListFileName))
                {
                    string servicePath = Path.GetDirectoryName(System.Reflection.Assembly.GetEntryAssembly().Location);
                    imeiListFileName = Path.Combine(servicePath, imeiListFileNameOriginal);

                    if (!File.Exists(imeiListFileName))
                    {
                        log.ErrorFormat("Imei list file {0} not exists.", imeiListFileNameOriginal);
                        return imeiSet;
                    }
                }
            }
            catch (Exception e)
            {
                log.Error(String.Format("Exception on loading Imei list file {0}.", imeiListFileNameOriginal), e);
                return imeiSet;
            }
            
            using (StreamReader reader = new StreamReader(File.OpenRead(imeiListFileName)))
            {
                int i = 0;
                while(!reader.EndOfStream)
                {
                    string imei = reader.ReadLine();
                    if(!string.IsNullOrEmpty(imei))
                    {
                        imeiSet.Add(imei);
                        i++;
                    }
                }
                log.InfoFormat("Загружено {0} imei из файла {1}", i, imeiListFileName);
            }

            return imeiSet;
        }
    }
}
