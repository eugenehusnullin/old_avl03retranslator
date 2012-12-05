using System.Globalization;
using System.ServiceProcess;
using System.Threading;
using TcpServer.Core;

namespace TcpServer.Retranslator.Service2
{
    public partial class Glonass2 : ServiceBase
    {
        public Glonass2()
        {
            var appOptions = new AppOptions();

            InitializeComponent();

            if (!System.Diagnostics.EventLog.SourceExists("Retranslator Service 2"))
            {
                System.Diagnostics.EventLog.CreateEventSource("Retranslator Service 2", "Glonass Log");
            }
            eventLog.Source = "Retranslator Service 2";
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
        }
    }
}
