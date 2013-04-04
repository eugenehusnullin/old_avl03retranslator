using System;
using TcpServer.Core.Properties;

namespace TcpServer.Core.Mintrans
{
    public class MintransMoscowRegionSettings : IUnifiedProtocolSettings
    {
        public MintransMoscowRegionSettings()
        {
            this.Url = Settings.Default.MintransMoscowRegion_Url;
            this.UserName = Settings.Default.MintransMoscowRegion_UserName;
            this.Password = Settings.Default.MintransMoscowRegion_Password;
            this.ImeiListFileName = Settings.Default.MintransMoscowRegion_ImeiListFileName;
            this.Enabled = Settings.Default.MintransMoscowRegion_Enabled;
            this.LoggerName = Settings.Default.MintransMoscowRegion_LoggerName;
        }

        public string Url { get; private set; }
        public string UserName { get; private set; }
        public string Password { get; private set; }
        public string ImeiListFileName { get; private set; }
        public bool Enabled { get; private set; }
        public string LoggerName { get; private set; }
    }
}