using System;
using System.Diagnostics;
using System.IO;
using System.Net;
using System.Net.Sockets;
using System.Text;
using System.Threading;

namespace TcpServer.Core
{
    public class RetranslatorGlonass
    {
        public RetranslatorGlonass(string srcIpAddress, int srcPort, string dstHost, int dstPort) : this(srcIpAddress, srcPort, dstHost, dstPort, null, new Options { LogPath = "RetranslatorLog" }) { }
        public RetranslatorGlonass(string srcIpAddress, int srcPort, string dstHost, int dstPort, EventLog eventLog, Options options)
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
            // TeleMaxima
            RetranslatorTelemaxima.Init(Logger);

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

                if (Options.UseFeedBack)
                {
                    var tcpClients = new TcpClients(blockClient, serverClient);
                    var thread = new Thread(() => DoFeedback(tcpClients));
                    thread.Start();
                }

                var bytes = new Byte[1000];
                int i;
                while (State != ServiceState.Stoping && (i = blockStream.Read(bytes, 0, bytes.Length)) != 0)
                {
                    var log = new StringBuilder();

                    if (!blockStream.CanRead) throw new Exception("Невозможно получить данные от блока");

                    var srcData = Encoding.ASCII.GetString(bytes, 0, i);

                    log.AppendFormat("{0}src: {1}", Environment.NewLine, srcData);

                    var packetString = srcData;

                    var isPacket = IsPacket(srcData);
                    if (isPacket)
                    {
                        var basePacket = BasePacket.GetFromGlonass(srcData);
                        ThreadPool.QueueUserWorkItem(new WaitCallback(RetranslatorTelemaxima.DoMaxima), basePacket);
                        packetString = basePacket.ToPacketGps();
                    }

                    if (!serverStream.CanWrite) throw new Exception("Невозможно отправить данные на сервер");

                    var dstData = Encoding.ASCII.GetBytes(packetString);
                    lock (_thisLock)
                    {
                        serverStream.Write(dstData, 0, dstData.Length);
                    }
                   
                    log.AppendFormat("dst: {0}", packetString);


                    
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

        private bool IsPacket(string srcData)
        {
            return srcData.StartsWith("$$");
        }

        private void DoFeedback(object clientObject)
        {
            TcpClient srcClient = null;
            NetworkStream srcStream = null;

            TcpClient blockClient = null;
            NetworkStream dstStream = null;

            try
            {
                var clients = (TcpClients)clientObject;

                srcClient = clients.Server;
                srcStream = srcClient.GetStream();

                blockClient = clients.Block;
                dstStream = blockClient.GetStream();

                var bytes = new Byte[256];
                int i;
                while (State != ServiceState.Stoping && (i = srcStream.Read(bytes, 0, bytes.Length)) != 0)
                {
                    var commandLog = new StringBuilder();

                    if (!srcStream.CanRead) throw new Exception("Невозможно получить команды от сервера");

                    var srcData = Encoding.ASCII.GetString(bytes, 0, i);

                    commandLog.AppendFormat("{0}src: {1}", Environment.NewLine, srcData);

                    if (!dstStream.CanWrite) throw new Exception("Невозможно отправить команды блоку");

                    var dstData = Encoding.ASCII.GetBytes(srcData);
                    lock (_thisLock)
                    {
                        dstStream.Write(dstData, 0, dstData.Length);
                    }

                    commandLog.AppendFormat("dst: {0}", srcData);

                    Logger.CommandWriteLine(commandLog.ToString());
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

            if (srcStream != null) srcStream.Close();
            if (dstStream != null) dstStream.Close();

            if (srcClient != null) srcClient.Close();
            if (blockClient != null) blockClient.Close();
        }
    }
}