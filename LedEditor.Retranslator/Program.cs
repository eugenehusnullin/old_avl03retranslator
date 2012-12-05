using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Text;
using System.Threading;
using TcpServer.Core;

namespace LedEditor.Retranslator
{
    class Program
    {
        static void Main(string[] args)
        {
            Thread.CurrentThread.CurrentCulture = new CultureInfo("en-Us");
            //var retranslator = new RetranslatorGlonass("195.206.252.247", 10181, "77.74.50.78", 20141);
            var options = new Options();
            options.LogPath = "LedEditorRetranslatorLog";
            options.UseFeedBack = true;
            var retranslator = new LedEditorRetranslator("192.168.1.101", 20146, "123.196.114.95", 5059, null, options);
            retranslator.Start();

            Console.ReadKey();
        }
    }
}
