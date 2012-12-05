using System.Diagnostics;
using System.Globalization;
using System.ServiceProcess;
using System.Threading;
using TcpServer.Core;

namespace TcpServer.Retranslator.Service.Wialon
{
    public partial class Wialon : ServiceBase
    {
        public Wialon()
        {
            var appOptions = new AppOptions();

            InitializeComponent();

            if (!EventLog.SourceExists("Retranslator wialon"))
            {
                EventLog.CreateEventSource("Retranslator wialon", "Glonass Log");
            }
            eventLog.Source = "Retranslator wialon";
            eventLog.Log = "Glonass Log";

            var options = new Options();
            options.LogPath = appOptions.LogPath;

            Thread.CurrentThread.CurrentCulture = new CultureInfo("en-Us");
            RetranslatorWialon = new RetranslatorWialon(appOptions.SrcHost, appOptions.SrcPort, appOptions.DstHost, appOptions.DstPort, eventLog, options);
        }

        private RetranslatorWialon RetranslatorWialon { get; set; }

        protected override void OnStart(string[] args)
        {
            RetranslatorWialon.Start();
        }

        protected override void OnStop()
        {
            RetranslatorWialon.Stop();
        }
    }
}
