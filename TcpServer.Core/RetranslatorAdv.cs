using System;
using System.Diagnostics;
using System.IO;
using System.Net;
using System.Net.Sockets;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading;
using System.Linq;

namespace TcpServer.Core
{
    public class RetranslatorAdv
    {
        public RetranslatorAdv(string srcIpAddress, int srcPort, string dstHost, int dstPort) : this(srcIpAddress, srcPort, dstHost, dstPort, null, new Options { LogPath = "RetranslatorAdvLog" }) { }
        public RetranslatorAdv(string srcIpAddress, int srcPort, string dstHost, int dstPort, EventLog eventLog, Options options)
        {
            SrcHost = srcIpAddress;
            SrcPort = srcPort;

            DstHost = dstHost;
            DstPort = dstPort;

            Options = options;
            Logger = new Logger(eventLog, options.LogPath);
        }

        public string SrcHost { get; set; }
        public int SrcPort { get; set; }

        public string DstHost { get; set; }
        public int DstPort { get; set; }

        public const int interval = 30 * 1000;

        public Options Options { get; set; }
        public Logger Logger { get; set; }

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
            //var advPacket = GetAdvPacket(2033, 42, new byte[0]);
            //var packet = GetPppPacket(advPacket);
            //Console.WriteLine("packet > " + packet);

            var localAddr = IPAddress.Parse(SrcHost);
            Console.WriteLine("Start listing > ");

            var tcpListener = new TcpListener(localAddr, SrcPort);
            try
            {
                // Start listening for client requests.
                tcpListener.Start();

                // Enter the listening loop.
                while (State != ServiceState.Stoping)
                {
                    var tcpClient = tcpListener.AcceptTcpClient();
                    ThreadPool.QueueUserWorkItem(DoProcess, tcpClient);
                }
            }
            catch (Exception e)
            {
                Console.WriteLine("Exception: {0}", e);
            }
            finally
            {
                tcpListener.Stop();
            }

            Console.WriteLine("\nHit enter to continue...");
            Console.Read();
        }

        private void DoProcess(object blockClientObject)
        {
            TcpClient blockClient = null;
            NetworkStream blockStream = null;

            TcpClient serverClient = null;
            NetworkStream serverStream = null;



            try
            {
                Console.WriteLine("Новый клиент... - > " + DateTime.Now.ToString());

                blockClient = (TcpClient)blockClientObject;
                blockStream = blockClient.GetStream();

                serverClient = new TcpClient(DstHost, DstPort);
                serverStream = serverClient.GetStream();

                var buffer = new Byte[256];
                var i = blockStream.Read(buffer, 0, buffer.Length);
                var device = Encoding.ASCII.GetString(buffer, 0, i);
                Console.WriteLine("Получаем информацию об устройстве: {0}", device);
                File.AppendAllText("clients.log", device, Encoding.Default);

                const string pattern = @"ADV[\d-.]*\sDevice number:\s(\d*).";
                var regex = new Regex(pattern, RegexOptions.IgnoreCase);
                var match = regex.Match(device);

                int deviceId;
                int.TryParse(match.Groups[1].Value, out deviceId);

                if (deviceId > 0)
                {
                    if (Options.UseFeedBack)
                    {
                        var tcpClients = new TcpClients(blockClient, serverClient);
                        tcpClients.DeviceId = deviceId;
                        var thread = new Thread(() => DoFeedback(tcpClients));
                        thread.Start();
                        //ThreadPool.QueueUserWorkItem(DoFeedback, tcpClients);
                    }

                    while (State != ServiceState.Stoping)
                    {
                        var advPacket = GetAdvPacket(deviceId, 01, 42, new byte[0]);
                        var packet = GetPppPacket(advPacket);
                        File.AppendAllText("send.log", packet + Environment.NewLine, Encoding.Default);

                        var login = PPP.GetBytesFromByteString(packet);
                        blockStream.Write(login, 0, login.Length);
                        Console.WriteLine("Отправляем пакет на чтение 42 регистра: " + packet);

                        var receiveBuffer = new Byte[256];
                        var size = blockStream.Read(receiveBuffer, 0, receiveBuffer.Length);

                        var data = ByteHelper.GetStringFromBytes(receiveBuffer.Take(size));
                        File.AppendAllText("receive.log", data + Environment.NewLine, Encoding.Default);
                        Console.WriteLine("Получаем результат чтения 42 регистра: {0}", data);

                        var packetBytes = PPP.GetBytesFromByteString(data);
                        var unpackData = PPP.UnpackData(packetBytes);
                        var basePacket = BasePacket.GetFromAdv(unpackData);
                        var gpsPacket = basePacket.ToPacketGps();

                        var dstData = Encoding.ASCII.GetBytes(gpsPacket);
                        serverStream.Write(dstData, 0, dstData.Length);
                        File.AppendAllText("retranslate.log", gpsPacket + Environment.NewLine, Encoding.Default);
                        Console.WriteLine("Отправлен пакет: {0}", gpsPacket);



                        Console.WriteLine("Ждем {0} секунд", interval / 1000);
                        Thread.Sleep(interval);
                        Console.WriteLine("Переходим к началу цикла ...");
                        Console.WriteLine();
                    }
                }
                else
                {
                    Console.WriteLine("Не определен id девайса");
                }
            }
            catch (Exception e)
            {
                Console.WriteLine("Exception: {0}", e);
            }

            if (blockStream != null) blockStream.Close();
            if (blockClient != null) blockClient.Close();

            if (serverStream != null) serverStream.Close();
            if (serverClient != null) serverClient.Close();
        }

        private void DoFeedback(object clientObject)
        {


            TcpClient srcClient = null;
            NetworkStream srcStream = null;

            TcpClient dstClient = null;
            NetworkStream dstStream = null;

            try
            {
                var clients = (TcpClients)clientObject;

                var deviceId = clients.DeviceId;

                srcClient = clients.Server;
                srcStream = srcClient.GetStream();

                dstClient = clients.Block;
                dstStream = dstClient.GetStream();

                var bytes = new Byte[256];
                int i;
                while (State != ServiceState.Stoping && (i = srcStream.Read(bytes, 0, bytes.Length)) != 0)
                {
                    var commandLog = new StringBuilder();

                    if (!srcStream.CanRead) throw new Exception("Невозможно получить команды от сервера");

                    var srcData = Encoding.ASCII.GetString(bytes, 0, i);

                    commandLog.AppendFormat("{0}src: {1}", Environment.NewLine, srcData);

                    if (!dstStream.CanWrite) throw new Exception("Невозможно отправить команды блоку");

                    var cmd = string.Empty;
                    if (srcData == "*000000,025,A,1#")
                    {
                        cmd = "Y";
                    }
                    else
                    {
                        cmd = "N";
                    }
                    var advPacket = GetAdvPacket(deviceId, 02, 295, Encoding.ASCII.GetBytes(cmd));
                    var packet = GetPppPacket(advPacket);
                    var login = PPP.GetBytesFromByteString(packet);
                    lock (_thisLock)
                    {
                        dstStream.Write(login, 0, login.Length);
                    }

                    commandLog.AppendFormat("dst: {0}", packet);

                    Logger.CommandWriteLine(commandLog.ToString());
                }
            }
            catch (Exception ex)
            {
                Logger.ErrorWriteLine(ex);
            }

            if (srcStream != null) srcStream.Close();
            if (dstStream != null) dstStream.Close();

            if (srcClient != null) srcClient.Close();
            if (dstClient != null) dstClient.Close();
        }

        public static string GetAdvPacket(int deviceId, byte cmd, int register, byte[] data)
        {
            var advPacket = "00000000"; // AddrFROM

            var deviceIdHex = BitConverter.GetBytes(deviceId);
            advPacket += ByteHelper.GetStringFromBytes(deviceIdHex); // AddrTO

            advPacket += ByteHelper.GetStringFromBytes(new[] { cmd }); // CMD Type

            var registerHex = BitConverter.GetBytes(register);
            advPacket += ByteHelper.GetStringFromBytes(registerHex); // Register
            advPacket += "01"; // CountReg

            var dataLenHex = BitConverter.GetBytes((short)data.Length);
            advPacket += ByteHelper.GetStringFromBytes(dataLenHex); // DataLen
            advPacket += ByteHelper.GetStringFromBytes(data);
            return advPacket;
        }

        private static string GetPppPacket(string advPacket)
        {
            var ppp = new PPP();
            var packet = ppp.Make_PPP_Packet(advPacket);
            packet = ppp.PackData(packet);
            return packet;
        }
    }
}