using System.Globalization;
using System.Threading;
using TcpServer.Core;
using TcpServer.Core.async;

namespace TcpServer.RetranslatorTest
{
    class Program
    {
        static void Main(string[] args)
        {
            Thread.CurrentThread.CurrentCulture = new CultureInfo("en-Us");
            //var retranslator = new RetranslatorGlonass("84.234.58.229", 20141, "77.74.50.78", 20141);
            //var retranslator = new RetranslatorGlonass("192.168.1.5", 20141, "77.74.50.78", 20141);
            //retranslator.Start();

            ConnectionsAccepter async = new ConnectionsAccepter("31.31.20.193", 20141, "77.74.50.78", 20141);

            Thread thread = new Thread(() => async.start());
            thread.Start();


            System.Console.ReadLine();
            async.stop();
        }
    }
}
