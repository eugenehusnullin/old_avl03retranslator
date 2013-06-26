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
            var async = new AsyncRetranslator("127.0.0.1", 4805, "127.0.0.1", 20401);

            async.start();
            System.Console.ReadLine();
            async.stop();
        }
    }
}
