using System.Net.Sockets;

namespace TcpServer.Core
{
    public class TcpClients
    {
        public TcpClients(TcpClient block, TcpClient server) : this(block, server, 0) { }
        public TcpClients(TcpClient block, TcpClient server, int deviceId)
        {
            Block = block;
            Server = server;
            DeviceId = deviceId;
        }

        public TcpClient Block { get; private set; }
        public TcpClient Server { get; private set; }

        public int DeviceId { get; set; }
    }
}