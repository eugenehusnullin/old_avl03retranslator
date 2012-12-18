using System.ServiceProcess;

namespace TcpServer.Retranslator.Service2
{
    static class Program
    {
        /// <summary>
        /// The main entry point for the application.
        /// </summary>
        static void Main()
        {
            var ServicesToRun = new ServiceBase[]
            { 
                new Glonass2()
            };
            ServiceBase.Run(ServicesToRun);
        }
    }
}
