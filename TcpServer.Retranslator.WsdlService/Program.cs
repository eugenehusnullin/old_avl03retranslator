using System.ServiceProcess;

namespace TcpServer.Retranslator.Service.Wsdl
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
                new Wsdl() 
            };
            ServiceBase.Run(ServicesToRun);
        }
    }
}
