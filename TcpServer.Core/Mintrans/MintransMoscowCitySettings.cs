using TcpServer.Core.Properties;

namespace TcpServer.Core.Mintrans
{
    public class MintransMoscowCitySettings : IUnifiedProtocolSettings
    {
        public MintransMoscowCitySettings()
        {
            this.Url = Settings.Default.MintransMoscowCity_Url;
            this.UserName = Settings.Default.MintransMoscowCity_UserName;
            this.Password = Settings.Default.MintransMoscowCity_Password;
            this.ImeiListFileName = Settings.Default.MintransMoscowCity_ImeiListFileName;
            this.Enabled = Settings.Default.MintransMoscowCity_Enabled;
        }

        public string Url { get; private set; }
        public string UserName { get; private set; }
        public string Password { get; private set; }
        public string ImeiListFileName { get; private set; }
        public bool Enabled { get; private set; }
    }
}
