using System;
using System.Diagnostics;
using System.IO;
using System.Net;
using System.Net.Sockets;
using System.Text;
using System.Threading;

namespace TcpServer.Core
{
    public class RetranslatorTeltonika
    {
        public RetranslatorTeltonika(string srcIpAddress, int srcPort, string dstHost, int dstPort) : this(srcIpAddress, srcPort, dstHost, dstPort, null, new Options { LogPath = "RetranslatorTeltonikaLog" }) { }
        public RetranslatorTeltonika(string srcIpAddress, int srcPort, string dstHost, int dstPort, EventLog eventLog, Options options)
        {
            SrcHost = srcIpAddress;
            SrcPort = srcPort;

            DstHost = dstHost;
            DstPort = dstPort;

            Options = options;
            Logger = new Logger(eventLog, options.LogPath);
        }

        public Options Options { get; set; }
        public Logger Logger { get; set; }

        public string SrcHost { get; private set; }
        public int SrcPort { get; private set; }

        public string DstHost { get; private set; }
        public int DstPort { get; private set; }

        public ServiceState State { get; set; }
        public enum ServiceState
        {
            Starting, Started, Stoping, Stoped
        }

        public Thread Thread { get; set; }

        public void Start()
        {
            State = ServiceState.Starting;
            Thread = new Thread(DoWork);
            Thread.Start();
        }
        public void Stop()
        {
            State = ServiceState.Stoping;
            Thread.Abort();
        }

        private void DoWork()
        {
            var localAddr = IPAddress.Parse(SrcHost);

            var tcpListener = new TcpListener(localAddr, SrcPort);
            try
            {
                tcpListener.Start();

                while (State != ServiceState.Stoping)
                {
                    var tcpClients = tcpListener.AcceptTcpClient();
                    var thread = new Thread(() => DoTcpClient(tcpClients));
                    thread.Start();
                }
            }
            catch (SocketException e)
            {
                Logger.ErrorWriteLine(e);
            }

            tcpListener.Stop();
        }

        private void DoTcpClient(object blockClientObject)
        {

            TcpClient blockClient = null;
            NetworkStream blockStream = null;

            TcpClient serverClient = null;
            NetworkStream serverStream = null;

            try
            {
                blockClient = (TcpClient)blockClientObject;
                blockStream = blockClient.GetStream();

                serverClient = new TcpClient(DstHost, DstPort);
                serverStream = serverClient.GetStream();

                var blockReadBuffer = new Byte[1000];
                var serverReadBuffer = new Byte[1000];
                int i;
                while (State != ServiceState.Stoping && (i = blockStream.Read(blockReadBuffer, 0, blockReadBuffer.Length)) != 0)
                {
                    var log = new StringBuilder();
                    
                    var blockPacket = Encoding.ASCII.GetString(blockReadBuffer, 0, i);
                    log.AppendFormat("{0}src: read{1}", Environment.NewLine, blockPacket);
                    
                    if (!serverStream.CanWrite) throw new Exception("Невозможно отправить данные на сервер");

                    var serverSendData = Encoding.ASCII.GetBytes(blockPacket);
                    serverStream.Write(serverSendData, 0, serverSendData.Length);
                    log.AppendFormat("{0}dst: send{1}", Environment.NewLine, blockPacket);

                    if (!serverStream.CanRead) throw new Exception("Невозможно получить данные от сервера");
                    var k = serverStream.Read(serverReadBuffer, 0, serverReadBuffer.Length);
                    var dstReadData = Encoding.ASCII.GetString(serverReadBuffer, 0, k);
                    log.AppendFormat("{0}dst: read{1}", Environment.NewLine, dstReadData);

                    var blockSendData = Encoding.ASCII.GetBytes(dstReadData);

                    if (!blockStream.CanWrite) throw new Exception("Невозможно отправить данные блоку");
                    blockStream.Write(blockSendData, 0, blockSendData.Length);
                    log.AppendFormat("{0}src: send{1}", Environment.NewLine, dstReadData);

                    Logger.PacketWriteLine(log.ToString());

                }
            }
            catch (IOException ex)
            {
                Logger.WarningWriteLine(ex);
            }
            catch (Exception ex)
            {
                Logger.ErrorWriteLine(ex);
            }

            if (blockStream != null) blockStream.Close();
            if (serverStream != null) serverStream.Close();

            if (blockClient != null) blockClient.Close();
            if (serverClient != null) serverClient.Close();
        }
    }
}