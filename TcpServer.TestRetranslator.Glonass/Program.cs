using System;
using System.Globalization;
using System.Threading;
using TcpServer.Core;

namespace TcpServer.TestRetranslator.Glonass
{
    class Program
    {
        static void Main(string[] args)
        {
            Thread.CurrentThread.CurrentCulture = new CultureInfo("en-Us");
            //var retranslator = new RetranslatorGlonass("195.206.252.247", 10181, "77.74.50.78", 20141);
            var options = new Options();
            options.LogPath = "RetranslatorLog";
            options.UseFeedBack = true;
            var retranslator = new RetranslatorGlonass("192.168.1.5", 10181, "77.74.50.78", 20141, null, options);
            retranslator.Start();

            Console.ReadKey();
        }
    }
}
