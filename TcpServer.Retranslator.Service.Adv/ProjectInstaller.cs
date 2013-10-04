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

            AdvInstaller.ServiceName = GetConfigurationValue("ServiceName");
            AdvInstaller.Description = GetConfigurationValue("ServiceName");
            AdvInstaller.DisplayName = GetConfigurationValue("ServiceName");
        }

        private string GetConfigurationValue(string key)
        {
            Assembly service = Assembly.GetAssembly(typeof(RetranslatorAdv));
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

        private void AdvInstaller_AfterInstall(object sender, System.Configuration.Install.InstallEventArgs e)
        {

        }

        private void serviceProcessInstaller1_AfterInstall(object sender, System.Configuration.Install.InstallEventArgs e)
        {

        }
    }
}
