using System.Globalization;
using System.ServiceProcess;
using System.Threading;
using TcpServer.Core;
using TcpServer.Core.async.retranslator;

namespace TcpServer.Retranslator.Service
{
    public partial class GlonassAsync : ServiceBase
    {
        public GlonassAsync()
        {
            var appOptions = new AppOptions();

            InitializeComponent();

            //if (!System.Diagnostics.EventLog.SourceExists("Retranslator Service"))
            //{
            //    System.Diagnostics.EventLog.CreateEventSource("Retranslator Service", "Glonass Log");
            //}
            eventLog.Source = "Retranslator Service";
            eventLog.Log = "Glonass Log";

            var options = new Options();
            options.LogPath = appOptions.LogPath;
            options.UseFeedBack = appOptions.UseFeedBack;

            Thread.CurrentThread.CurrentCulture = new CultureInfo("en-Us");
            //RetranslatorGlonass = new RetranslatorGlonass(appOptions.SrcHost, appOptions.SrcPort, appOptions.DstHost, appOptions.DstPort, eventLog, options);
            asyncRetranslator = new AsyncRetranslator(appOptions.SrcHost, appOptions.SrcPort, appOptions.DstHost, appOptions.DstPort);
        }

        //private RetranslatorGlonass RetranslatorGlonass { get; set; }
        private AsyncRetranslator asyncRetranslator;

        protected override void OnStart(string[] args)
        {
            //RetranslatorGlonass.Start();
            asyncRetranslator.start();
        }

        protected override void OnStop()
        {
            //RetranslatorGlonass.Stop();
            asyncRetranslator.stop();
        }
    }
}
