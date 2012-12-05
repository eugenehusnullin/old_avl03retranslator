using System;
using System.Configuration;

namespace TcpServer.Retranslator.Service
{
    public  class AppOptions
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

            var dstHost = ConfigurationManager.AppSettings["DstHost"];
            if (string.IsNullOrEmpty(dstHost))
            {
                throw new ArgumentException("Не создан параметр", "DstHost");
            }
            DstHost = dstHost;

            var dstPortString = ConfigurationManager.AppSettings["DstPort"];
            if (string.IsNullOrEmpty(dstPortString))
            {
                throw new ArgumentException("Не создан параметр", "DstPort");
            }

            int dstPort;
            if (!int.TryParse(dstPortString, out dstPort))
            {
                throw new ArgumentException("Параметр должен быть целым числом", "DstPort");
            }
            DstPort = dstPort;

            var logPath = ConfigurationManager.AppSettings["LogPath"];
            if (string.IsNullOrEmpty(logPath))
            {
                throw new ArgumentException("Не создан параметр", "LogPath");
            }
            LogPath = logPath;

            var useFeedBackString = ConfigurationManager.AppSettings["UseFeedBack"];
            if (string.IsNullOrEmpty(useFeedBackString))
            {
                throw new ArgumentException("Не создан параметр", "UseFeedBack");
            }

            bool useFeedBack;
            if (!bool.TryParse(useFeedBackString, out useFeedBack))
            {
                throw new ArgumentException("Параметр должен быть true или false", "UseFeedBack");
            }
            UseFeedBack = useFeedBack;
        }

        public  string SrcHost { get; set; }
        public  int SrcPort { get; set; }

        public  string DstHost { get; set; }
        public  int DstPort { get; set; }

        public string LogPath { get; set; }
        public bool UseFeedBack { get; set; }
    }                
}