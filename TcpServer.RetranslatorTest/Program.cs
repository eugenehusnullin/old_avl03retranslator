using System.Globalization;
using System.Threading;
using TcpServer.Core;

namespace TcpServer.RetranslatorTest
{
    class Program
    {
        static void Main(string[] args)
        {
            Thread.CurrentThread.CurrentCulture = new CultureInfo("en-Us");
            //var retranslator = new RetranslatorGlonass("84.234.58.229", 20141, "77.74.50.78", 20141);
            var retranslator = new RetranslatorGlonass("192.168.1.5", 20141, "77.74.50.78", 20141);
            retranslator.Start();
        }
    }
}
