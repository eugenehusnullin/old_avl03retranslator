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
        private HashSet<string> imeiList;
        ILog log;

        public ImeiList(IUnifiedProtocolSettings settings)
        {
            this.imeiList = new HashSet<string>();
            this.settings = settings;
            this.log = LogManager.GetLogger(settings.LoggerName);

            if (settings.Enabled)
            {
                imeiList = ImeiListLoader.loadImeis(log, settings.ImeiListFileName);
            }
        }

        public bool Contains(string imei)
        {
            return this.imeiList.Contains(imei);
        }
    }
}