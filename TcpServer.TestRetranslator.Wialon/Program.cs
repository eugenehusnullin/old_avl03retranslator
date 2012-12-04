using System;
using System.Globalization;
using System.Threading;
using TcpServer.Core;

namespace TcpServer.TestRetranslator.Wialon
{
    class Program
    {
        static void Main(string[] args)
        {
            Thread.CurrentThread.CurrentCulture = new CultureInfo("en-Us");
            //var retranslator = new RetranslatorGlonass("195.206.252.247", 10181, "77.74.50.78", 20141);
            var options = new Options();
            options.LogPath = "RetranslatorWialonlLog";
            options.UseFeedBack = true;
            //var retranslator = new RetranslatorWialon("192.168.1.105", 20147, "89.108.72.236",5050);
            var retranslator = new RetranslatorWialon("192.168.1.105", 20146, "195.206.252.247", 20146);
            retranslator.Start();

            Console.ReadKey();
        }
    }
}
