using log4net;
using System;
using System.Collections.Generic;
using System.IO;

namespace TcpServer.Core.Mintrans
{
    public class ImeiList
    {
        private IUnifiedProtocolSettings settings;
        private HashSet<string> imeiList;
        ILog log;

        public ImeiList(IUnifiedProtocolSettings settings)
        {
            this.settings = settings;
            this.log = LogManager.GetLogger(settings.LoggerName);
            this.LoadList();
        }

        private void LoadList()
        {
            string imeiListFileName = this.settings.ImeiListFileName;

            try
            {
                if (!File.Exists(imeiListFileName))
                {
                    string servicePath = Path.GetDirectoryName(System.Reflection.Assembly.GetEntryAssembly().Location);
                    imeiListFileName = Path.Combine(servicePath, this.settings.ImeiListFileName);

                    if (!File.Exists(imeiListFileName))
                    {
                        log.ErrorFormat("Imei list file {0} not exists.", settings.ImeiListFileName);
                        return;
                    }
                }
            }
            catch (Exception e)
            {
                log.Error(String.Format("Exception on loading Imei list file {0}.", settings.ImeiListFileName), e);
                return;
            }

            this.imeiList = new HashSet<string>();
            using (StreamReader reader = new StreamReader(File.OpenRead(imeiListFileName)))
            {
                while(!reader.EndOfStream)
                {
                    string imei = reader.ReadLine();
                    if(!string.IsNullOrEmpty(imei))
                    {
                        this.imeiList.Add(imei);
                    }
                }
            }
        }

        public bool Contains(string imei)
        {
            return this.imeiList.Contains(imei);
        }
    }
}