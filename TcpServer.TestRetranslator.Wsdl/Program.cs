using System;
using System.Globalization;
using System.Threading;
using TcpServer.Core;

namespace TcpServer.TestRetranslator.Wsdl
{
    class Program
    {
        static void Main(string[] args)
        {
            Thread.CurrentThread.CurrentCulture = new CultureInfo("en-Us");
            //var retranslator = new RetranslatorGlonass("195.206.252.247", 10181, "77.74.50.78", 20141);
            var options = new Options();
            options.LogPath = "RetranslatorWsdlLog";
            options.UseFeedBack = true;
            var retranslator = new RetranslatorWsdl("192.168.1.5", 20145);
            retranslator.Start();

            Console.ReadKey();
        }
    }
}
