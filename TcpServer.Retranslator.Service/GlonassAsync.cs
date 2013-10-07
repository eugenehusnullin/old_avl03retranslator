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

            eventLog.Source = "Retranslator Service";
            eventLog.Log = "Glonass Log";

            var options = new Options();
            options.LogPath = appOptions.LogPath;
            options.UseFeedBack = appOptions.UseFeedBack;

            Thread.CurrentThread.CurrentCulture = new CultureInfo("en-Us");
            asyncRetranslator = new AsyncRetranslator(appOptions.SrcHost, appOptions.SrcPort, appOptions.DstHost, appOptions.DstPort);
        }
        
        private AsyncRetranslator asyncRetranslator;

        protected override void OnStart(string[] args)
        {
            asyncRetranslator.start();
        }

        protected override void OnStop()
        {
            asyncRetranslator.stop();
        }
    }
}
