using System;
using System.Configuration;
using System.Globalization;
using System.Threading;
using TcpServer.Core;
using TcpServer.Core.async.retranslator;

namespace TcpServer.RetranslatorTest
{
    class Program
    {
        static void Main(string[] args)
        {
            Thread.CurrentThread.CurrentCulture = new CultureInfo("en-Us");
            var async = new AsyncRetranslator(
                ConfigurationManager.AppSettings["SrcHost"],
                Convert.ToInt32(ConfigurationManager.AppSettings["SrcPort"]), 
                ConfigurationManager.AppSettings["DstHost"], 
                Convert.ToInt32(ConfigurationManager.AppSettings["DstPort"])
            );

            async.start();
            System.Console.ReadLine();
            async.stop();
        }
    }
}
