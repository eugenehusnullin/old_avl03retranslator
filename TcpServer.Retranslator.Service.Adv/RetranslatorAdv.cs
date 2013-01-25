using System.Globalization;
using System.ServiceProcess;
using System.Threading;
using TcpServer.Core;
using TcpServer.Core.async.retranslator;

namespace TcpServer.Retranslator.Service
{
    public partial class RetranslatorAdv : ServiceBase
    {
        public RetranslatorAdv()
        {
            var appOptions = new AppOptions();

            InitializeComponent();
            
            eventLog.Source = "Retranslator Service";
            eventLog.Log = "Retranslator Adv";

            var options = new Options();
            options.LogPath = appOptions.LogPath;
            options.UseFeedBack = appOptions.UseFeedBack;

            Thread.CurrentThread.CurrentCulture = new CultureInfo("en-Us");
            retranslatorAdv = new Core.RetranslatorAdv(appOptions.SrcHost, appOptions.SrcPort, appOptions.DstHost, appOptions.DstPort);
        }

        private TcpServer.Core.RetranslatorAdv retranslatorAdv;

        protected override void OnStart(string[] args)
        {
            retranslatorAdv.Start();
        }

        protected override void OnStop()
        {
            retranslatorAdv.Stop();
        }
    }
}
