using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using TcpServer.Core.Properties;

namespace TcpServer.Core.pilotka
{
    public class PilotkaSettings
    {
        public PilotkaSettings()
        {
            this.Enabled = Settings.Default.Pilotka_Enabled;
            this.ImeiListFileName = Settings.Default.Pilotka_ImeiListFileName;
            this.Url = Settings.Default.Pilotka_Url;
            this.LoggerName = Settings.Default.Pilotka_LoggerName;
        }

        public bool Enabled { get; private set; }
        public string ImeiListFileName { get; private set; }
        public string Url { get; private set; }
        public string LoggerName { get; private set; }
    }
}
