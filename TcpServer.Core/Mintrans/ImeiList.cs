using log4net;
using System;
using System.Collections.Generic;
using System.IO;
using TcpServer.Core.async.common;

namespace TcpServer.Core.Mintrans
{
    public class ImeiList
    {
        private IUnifiedProtocolSettings settings;
        private Dictionary<string, string> imeiDictionary;
        ILog log;

        public ImeiList(IUnifiedProtocolSettings settings)
        {
            this.settings = settings;
            this.log = LogManager.GetLogger(settings.LoggerName);
            this.imeiDictionary = new Dictionary<string, string>();

            if (settings.Enabled)
            {
                var set = ImeiListLoader.loadImeis(log, settings.ImeiListFileName);
                foreach (string csv in set)
                {
                    var strs = csv.Split(';');
                    imeiDictionary[strs[0]] = strs[1];
                }
            }
        }

        public string GetId(string imei)
        {
            if (imeiDictionary.ContainsKey(imei))
            {
                return imeiDictionary[imei];
            }
            else
            {
                return null;
            }
        }
    }
}