using System;
using System.Diagnostics;
using System.IO;
using System.Net;
using System.Net.Sockets;
using System.Text;
using System.Threading;
using System.Web.Services.Protocols;

namespace TcpServer.Core
{
    public class RetranslatorWialon
    {
        public RetranslatorWialon(string srcIpAddress, int srcPort, string dstHost, int dstPort) : this(srcIpAddress, srcPort, dstHost, dstPort, null, new Options { LogPath = "RetranslatorWialonLog" }) { }
        public RetranslatorWialon(string srcIpAddress, int srcPort, string dstHost, int dstPort, EventLog eventLog, Options options)
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

        public void Start()
        {
            State = ServiceState.Starting;
            var thread = new Thread(DoWork);
            thread.Start();
        }
        public void Stop()
        {
            State = ServiceState.Stoping;
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
                    tcpClients.ReceiveTimeout = 10 * 60 * 1000;
                    tcpClients.SendTimeout = 2 * 60 * 1000;
                    var thread = new Thread(() => DoTcpClient(tcpClients));
                    thread.Start();
                }
            }
            catch (SocketException e)
            {
                Logger.ErrorWriteLine(e);
            }
            finally
            {
                tcpListener.Stop();
            }
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



                var buffer = new byte[1000];

                int i;
                while (State != ServiceState.Stoping && (i = blockStream.Read(buffer, 0, buffer.Length)) != 0)
                {
                    if (!blockStream.CanRead) throw new Exception("Невозможно получить данные от блока");
                    var packet = ByteHelper.GetStringFromBytes(ByteHelper.GetBlockByCount(buffer, 0, i));

                    if (!serverStream.CanWrite) throw new Exception("Невозможно отправить данные на сервер");
                    serverStream.Write(buffer, 0, i);

                    var answerBuffer = new byte[1];
                    serverStream.Read(answerBuffer, 0, answerBuffer.Length);
                    blockStream.Write(answerBuffer, 0, answerBuffer.Length);

                    Logger.PacketWriteLine(string.Format("{0} src: {1}", Environment.NewLine, packet));
                }

                //// Buffer for reading data lenght
                //var bytes = new Byte[4];
                //int i;
                //lock (_thisLock)
                //{
                //    i = blockStream.Read(bytes, 0, bytes.Length);
                //}

                //if (i > 0)
                //{
                //    var bufferSize = BitConverter.ToInt32(bytes, 0);

                //    var buffer = new Byte[bufferSize];
                //    lock (_thisLock)
                //    {
                //        blockStream.Read(buffer, 0, buffer.Length);
                //    }

                //    var packet = ByteHelper.GetStringFromBytes(buffer);
                //    Logger.PacketWriteLine(packet);

                //    var basePacket = BasePacket.GetFromWialon(buffer);



                //    var resultBuffer = new[] { (byte)0x11 };
                //    lock (_thisLock)
                //    {
                //        blockStream.Write(resultBuffer, 0, resultBuffer.Length);
                //    }
                //}
                //Thread.Sleep(50);

            }
            catch (IOException ex)
            {
                Logger.WarningWriteLine(ex);
            }
            catch (SoapException ex)
            {
                Logger.WarningWriteLine(ex);
            }
            catch (WebException ex)
            {
                Logger.ErrorWriteLine(ex);
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