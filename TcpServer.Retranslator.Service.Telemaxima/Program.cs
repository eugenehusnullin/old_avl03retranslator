using System.ServiceProcess;

namespace TcpServer.Retranslator.Service
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
                new GlonassAsync()
            };
            ServiceBase.Run(ServicesToRun);
        }
    }
}
