using System;
using System.ComponentModel;
using System.Configuration;
using System.Reflection;

namespace TcpServer.Retranslator.Service
{
    [RunInstaller(true)]
    public partial class ProjectInstaller : System.Configuration.Install.Installer
    {
        public ProjectInstaller()
        {
            InitializeComponent();

            GlonassInstaller.ServiceName = GetConfigurationValue("ServiceName");
            GlonassInstaller.Description = GetConfigurationValue("ServiceName");
            GlonassInstaller.DisplayName = GetConfigurationValue("ServiceName");
        }

        private string GetConfigurationValue(string key)
        {
            Assembly service = Assembly.GetAssembly(typeof(GlonassAsync));
            Configuration config = ConfigurationManager.OpenExeConfiguration(service.Location);
            if (config.AppSettings.Settings[key] != null)
            {
                return config.AppSettings.Settings[key].Value;
            }
            else
            {
                throw new IndexOutOfRangeException
                    ("Settings collection does not contain the requested key: " + key);
            }
        }
    }
}
