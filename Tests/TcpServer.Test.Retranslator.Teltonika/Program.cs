using System;
using TcpServer.Core;

namespace TcpServer.Test.Retranslator.Teltonika
{
    class Program
    {
        static void Main(string[] args)
        {
            var options = new Options();
            options.LogPath = "RetranslatorTeltonikaLog";
            var retranslator = new RetranslatorTeltonika("192.168.1.101", 20148, "193.193.165.165", 20255, null, options);
            retranslator.Start();
            Console.ReadKey();
            retranslator.Stop();
        }
    }
}
