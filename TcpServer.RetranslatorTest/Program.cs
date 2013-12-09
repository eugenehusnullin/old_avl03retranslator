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
            var async = new AsyncRetranslator("195.206.252.236", 20181, "77.74.50.78", 20141);

            async.start();
            System.Console.ReadLine();
            async.stop();
        }
    }
}

/*
 * <add key="SrcHost" value="195.206.252.236" /> 
  <add key="SrcPort" value="20181" /> 
  <add key="DstHost" value="77.74.50.78" /> 
  <add key="DstPort" value="20141" /> 

*/