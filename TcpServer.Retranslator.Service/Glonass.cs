using System.Globalization;
using System.ServiceProcess;
using System.Threading;
using TcpServer.Core;

namespace TcpServer.Retranslator.Service
{
    public partial class Glonass : ServiceBase
    {
        public Glonass()
        {
            var appOptions = new AppOptions();

            InitializeComponent();

            if (!System.Diagnostics.EventLog.SourceExists("Retranslator Service"))
            {
                System.Diagnostics.EventLog.CreateEventSource("Retranslator Service", "Glonass Log");
            }
            eventLog.Source = "Retranslator Service";
            eventLog.Log = "Glonass Log";

            var options = new Options();
            options.LogPath = appOptions.LogPath;
            options.UseFeedBack = appOptions.UseFeedBack;

            Thread.CurrentThread.CurrentCulture = new CultureInfo("en-Us");
            RetranslatorGlonass = new RetranslatorGlonass(appOptions.SrcHost, appOptions.SrcPort, appOptions.DstHost, appOptions.DstPort, eventLog, options);
        }

        private RetranslatorGlonass RetranslatorGlonass { get; set; }

        protected override void OnStart(string[] args)
        {
            RetranslatorGlonass.Start();
        }

        protected override void OnStop()
        {
            RetranslatorGlonass.Stop();
        }
    }
}
