using System.Diagnostics;
using System.Globalization;
using System.ServiceProcess;
using System.Threading;
using OnlineMonitoring.ServerCore;
using OnlineMonitoring.ServerCore.Listners;

namespace OnlineMonitoring.Listner.Wialon
{
    public partial class Wialon : ServiceBase
    {
        public Wialon()
        {
            InitializeComponent();

            var appOptions = new AppOptions();

            if (!EventLog.SourceExists("Wialon Listner"))
            {
                EventLog.CreateEventSource("Wialon Listner", "Glonass Log");
            }
            eventLog.Source = "Wialon Listner";
            eventLog.Log = "Glonass Log";

            var options = new Options();
            options.ConnectionString = appOptions.ConnectionString;
            options.LogPath = appOptions.LogPath;

            Thread.CurrentThread.CurrentCulture = new CultureInfo("en-Us");
            WialonListner = new WialonListner(appOptions.SrcHost, appOptions.SrcPort, eventLog, options);
        }

        private WialonListner WialonListner { get; set; }

        protected override void OnStart(string[] args)
        {
            WialonListner.Start();
        }

        protected override void OnStop()
        {
            WialonListner.Stop();
        }
    }
}
