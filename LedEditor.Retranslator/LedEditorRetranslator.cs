using System;
using System.Diagnostics;
using System.IO;
using System.Net;
using System.Net.Sockets;
using System.Text;
using System.Threading;
using TcpServer.Core;

namespace LedEditor.Retranslator
{
    public class LedEditorRetranslator
    {
        public LedEditorRetranslator(string srcIpAddress, int srcPort, string dstHost, int dstPort) : this(srcIpAddress, srcPort, dstHost, dstPort, null, new Options { LogPath = "RetranslatorLog" }) { }
        public LedEditorRetranslator(string srcIpAddress, int srcPort, string dstHost, int dstPort, EventLog eventLog, Options options)
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

        private readonly object _thisLock = new object();

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


                var bytes = new Byte[1000];

                while (State != ServiceState.Stoping)
                {
                    var k = serverStream.Read(bytes, 0, bytes.Length);
                    blockStream.Write(bytes, 0, k);
                    Logger.PacketWriteLine("Принял " + ByteHelper.GetStringFromBytes(bytes, 0, k));

                    var i = blockStream.Read(bytes, 0, bytes.Length);
                    serverStream.Write(bytes, 0, i);
                    Logger.PacketWriteLine("Отправил " + ByteHelper.GetStringFromBytes(bytes, 0, i));
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