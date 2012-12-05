using TcpServer.Core;

namespace TcpServer.ListnerTest
{
    class Program
    {
        static void Main(string[] args)
        {
            var listner = new Listner("192.168.1.101", 20148);
            listner.Start();
        }
    }
}