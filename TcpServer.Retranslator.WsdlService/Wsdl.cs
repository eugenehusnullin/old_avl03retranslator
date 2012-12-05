using System.Globalization;
using System.ServiceProcess;
using System.Threading;
using TcpServer.Core;

namespace TcpServer.Retranslator.Service.Wsdl
{
    public partial class Wsdl : ServiceBase
    {
        public Wsdl()
        {
            InitializeComponent();

            var appOptions = new AppOptions();

            if (!System.Diagnostics.EventLog.SourceExists("Retranslator wsdl"))
            {
                System.Diagnostics.EventLog.CreateEventSource("Retranslator wsdl", "Glonass Log");
            }
            eventLog.Source = "Retranslator wsdl";
            eventLog.Log = "Glonass Log";

            var options = new Options();
            options.LogPath = appOptions.LogPath;

            Thread.CurrentThread.CurrentCulture = new CultureInfo("en-Us");
            RetranslatorWsdl = new RetranslatorWsdl(appOptions.SrcHost, appOptions.SrcPort, eventLog, options);
        }

        public RetranslatorWsdl RetranslatorWsdl { get; set; }

        protected override void OnStart(string[] args)
        {
            RetranslatorWsdl.Start();
        }

        protected override void OnStop()
        {
            RetranslatorWsdl.Stop();
        }
    }
}
