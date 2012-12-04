using System;
using System.Configuration;

namespace OnlineMonitoring.Listner.Wialon
{
    public class AppOptions
    {
        public AppOptions()
        {
            var srcHost = ConfigurationManager.AppSettings["SrcHost"];
            if (string.IsNullOrEmpty(srcHost))
            {
                throw new ArgumentException("Не создан параметр", "SrcHost");
            }
            SrcHost = srcHost;

            var srcPortString = ConfigurationManager.AppSettings["SrcPort"];
            if (string.IsNullOrEmpty(srcPortString))
            {
                throw new ArgumentException("Не создан параметр", "SrcPort");
            }

            int srcPort;
            if (!int.TryParse(srcPortString, out srcPort))
            {
                throw new ArgumentException("Параметр должен быть целым числом", "SrcPort");
            }
            SrcPort = srcPort;

            var logPath = ConfigurationManager.AppSettings["LogPath"];
            if (string.IsNullOrEmpty(logPath))
            {
                throw new ArgumentException("Не создан параметр", "LogPath");
            }
            LogPath = logPath;

            ConnectionString = ConfigurationManager.ConnectionStrings["Connection"].ConnectionString;
        }

        public string SrcHost { get; set; }
        public int SrcPort { get; set; }

        public string LogPath { get; set; }
        public string ConnectionString { get; set; }
    }
}