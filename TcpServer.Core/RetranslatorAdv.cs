using System;
using System.Diagnostics;
using System.Net;
using System.Net.Sockets;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading;
using System.Linq;
using log4net;
using System.IO;
using log4net.Config;
using System.Collections.Generic;

namespace TcpServer.Core
{
    public class RetranslatorAdv
    {
        private readonly string sjNumbersFileName = "sjNumbers.bin";
        private readonly DateTime restoreFrom = new DateTime(2012, 12, 23);
        private readonly DateTime restoreTo = new DateTime(2013, 01, 25);

        public RetranslatorAdv(string srcIpAddress, int srcPort, string dstHost, int dstPort) 
            : this(srcIpAddress, srcPort, dstHost, dstPort, null, new Options { LogPath = "RetranslatorAdvLog" }) 
        { }
        public RetranslatorAdv(string srcIpAddress, int srcPort, string dstHost, int dstPort, EventLog eventLog, Options options)
        {
            string appPath = Path.GetDirectoryName(System.Reflection.Assembly.GetEntryAssembly().Location);
            string log4netConfigPath = Path.Combine(appPath, "log4net.config");
            FileInfo fi = new FileInfo(log4netConfigPath);
            XmlConfigurator.ConfigureAndWatch(fi);
            log = LogManager.GetLogger(typeof(RetranslatorAdv));

            sjNumbersFilePath = Path.Combine(appPath, sjNumbersFileName);
            sjNumbers = DictionarySaver.Read(sjNumbersFilePath);

            SrcHost = srcIpAddress;
            SrcPort = srcPort;

            DstHost = dstHost;
            DstPort = dstPort;

            Options = options;
        }

        private string SrcHost { get; set; }
        private int SrcPort { get; set; }

        private string DstHost { get; set; }
        private int DstPort { get; set; }

        private const int interval = 30 * 1000;

        private Options Options { get; set; }

        private ServiceState State { get; set; }
        private enum ServiceState
        {
            Starting, Started, Stoping, Stoped
        }

        private readonly object _thisLock = new object();

        private ILog log;
        private Dictionary<int, int> sjNumbers;
        private string sjNumbersFilePath;
        public static object syncRoot = new object();

        private Thread thread;

        public void Start()
        {
            State = ServiceState.Starting;
            thread = new Thread(DoWork);
            thread.Start();

            log.Debug("App started.");
        }
        public void Stop()
        {
            DictionarySaver.Write(sjNumbers, sjNumbersFilePath);
            State = ServiceState.Stoping;
            tcpListener.Stop();

            log.Debug("App stoped.");
        }

        private TcpListener tcpListener;

        private void DoWork()
        {
            var localAddr = IPAddress.Parse(SrcHost);
            tcpListener = new TcpListener(localAddr, SrcPort);
            try
            {
                tcpListener.Start();
                while (State != ServiceState.Stoping)
                {
                    var tcpClient = tcpListener.AcceptTcpClient();
                    ThreadPool.QueueUserWorkItem(DoProcess, tcpClient);
                }
            }
            catch (Exception e)
            {
                log.Fatal("Cannot startup.", e);
            }
        }

        private void DoProcess(object blockClientObject)
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

                var buffer = new Byte[256];
                var i = blockStream.Read(buffer, 0, buffer.Length);
                var device = Encoding.ASCII.GetString(buffer, 0, i);
                log.DebugFormat("Получаем информацию об устройстве: {0}", device);
                log.Info(device);

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
                    }

                    // попытаться вычитать системный журнал за пропущенный месяц
                    int sjNumber = sjNumbers.ContainsKey(deviceId) ? sjNumbers[deviceId] : getSJNumber(deviceId, blockStream);

                    while (State != ServiceState.Stoping)
                    {
                        int cntSJ = 0;
                        while (true)
                        {
                            // вытягиваем в цикле более 30 записей
                            if (sjNumber != -1 && sjNumber > 1000 && cntSJ < 30)
                            {
                                int newSJNumber = processSJ(deviceId, sjNumber, blockStream, serverStream);
                                cntSJ += sjNumber - newSJNumber;
                                sjNumber = sjNumber == newSJNumber ? -1 : newSJNumber;
                            }
                            else
                            {
                                break;
                            }
                        }

                        var advPacket = GetAdvPacket(deviceId, 01, 42, new byte[0]);
                        var packet = GetPppPacket(advPacket);
                        log.Info(packet);

                        var login = PPP.GetBytesFromByteString(packet);
                        blockStream.Write(login, 0, login.Length);

                        var receiveBuffer = new Byte[256];
                        var size = blockStream.Read(receiveBuffer, 0, receiveBuffer.Length);

                        var data = ByteHelper.GetStringFromBytes(receiveBuffer.Take(size));
                        log.Info(data);

                        var packetBytes = PPP.GetBytesFromByteString(data);
                        var unpackData = PPP.UnpackData(packetBytes);
                        var basePacket = BasePacket.GetFromAdv(unpackData);
                        var gpsPacket = basePacket.ToPacketGps();

                        var dstData = Encoding.ASCII.GetBytes(gpsPacket);
                        serverStream.Write(dstData, 0, dstData.Length);
                        log.Info(gpsPacket);

                        if (sjNumber == -1 || sjNumber <= 1000)
                        {
                            Thread.Sleep(interval);
                        }
                    }
                }
                else
                {
                    log.Debug("Не определен id девайса");
                }
            }
            catch (Exception e)
            {
                log.Warn("Exception.", e);
            }

            if (blockStream != null) blockStream.Close();
            if (blockClient != null) blockClient.Close();

            if (serverStream != null) serverStream.Close();
            if (serverClient != null) serverClient.Close();
        }

        private int processSJ(int deviceId, int sjNumber, NetworkStream blockStream, NetworkStream serverStream)
        {
            var advPacket = GetAdvPacket(deviceId, 01, sjNumber, new byte[0]);
            var packet = GetPppPacket(advPacket);
            var login = PPP.GetBytesFromByteString(packet);
            blockStream.Write(login, 0, login.Length);
            log.DebugFormat("try reading sj line for deviceid={0}, sjnumber={1}", deviceId, sjNumber);

            var receiveBuffer = new Byte[10000];
            var size = blockStream.Read(receiveBuffer, 0, receiveBuffer.Length);
            var data = ByteHelper.GetStringFromBytes(receiveBuffer.Take(size));
            log.DebugFormat("get reading sj line for deviceid={0}, sjnumber={1}, value = {2}", deviceId, sjNumber, data);

            bool begin = false;
            int startIndex = -1;
            for (int i = 0; i < size; i++)
            {
                if (receiveBuffer[i] == 0x7E)
                {
                    // найдено начало пакета
                    begin = true;
                    startIndex = i;
                    break;
                }
            }

            int countReg = 0;
            if (begin)
            {
                if ((startIndex + 13) < size)
                {
                    if (receiveBuffer[startIndex + 13] == 0x81)
                    {
                        // удачное чтение регистра на устройстве

                        // узнаем кол-во считаных записей системного журнала
                        countReg = Convert.ToInt32(receiveBuffer[startIndex + 18]);
                        for (int i = 0; i < countReg; i++)
                        {
                            BasePacket basePacket;
                            try
                            {
                                basePacket = BasePacket.GetFromAdvSJ(deviceId, receiveBuffer, (startIndex + 21) + i * 60);
                            }
                            catch (Exception e)
                            {
                                log.ErrorFormat("error GetFromAdvSJ for deviceId={0}, trying number={1}. {2}", deviceId, i, e);
                                continue;
                            }

                            if (basePacket.RTC <= restoreTo && basePacket.RTC >= restoreFrom)
                            {
                                var gpsPacket = basePacket.ToPacketGps();
                                var dstData = Encoding.ASCII.GetBytes(gpsPacket);
                                serverStream.Write(dstData, 0, dstData.Length);
                                log.InfoFormat("!!!retranslated sj line = {0}, date={1}, for deviceId={2}", gpsPacket, basePacket.RTC, deviceId);
                            }
                            else
                            {
                                log.InfoFormat("dont retranslate sj line for deviceid={0}, because date is {1}", deviceId, basePacket.RTC);

                                if (basePacket.RTC < restoreFrom)
                                {
                                    countReg = sjNumber;
                                    break;
                                }
                            }
                        }
                    }
                    else
                    {
                        // не удачное чтение регистра
                        log.WarnFormat("error reading sj line for deviceid={0}, sjnumber={1}", deviceId, sjNumber);
                    }
                }
            }

            int newSJNumber = sjNumber - countReg - 1;
            lock (syncRoot)
            {
                sjNumbers[deviceId] = newSJNumber;
            }
            return newSJNumber;
        }

        private int getSJNumber(int deviceId, NetworkStream blockStream)
        {
            var advPacket = GetAdvPacket(deviceId, 01, 314, new byte[0]);
            var packet = GetPppPacket(advPacket);
            var login = PPP.GetBytesFromByteString(packet);
            blockStream.Write(login, 0, login.Length);
            log.DebugFormat("try reading 314 register for deviceid = {0}", deviceId);

            var receiveBuffer = new Byte[10000];
            var size = blockStream.Read(receiveBuffer, 0, receiveBuffer.Length);
            var data = ByteHelper.GetStringFromBytes(receiveBuffer.Take(size));
            log.DebugFormat("get reading 314 register from deviceid = {0}, value = {1}", deviceId, data);

            bool begin = false;
            int startIndex = -1;
            for (int k = 0; k < size; k++)
            {
                if (receiveBuffer[k] == 0x7E)
                {
                    // найдено начало пакета
                    begin = true;
                    startIndex = k;
                    break;
                }
            }

            int sjNumber = -1;
            if (begin)
            {
                if ((startIndex + 13) < size)
                {
                    if (receiveBuffer[startIndex + 13] == 0x81)
                    {
                        // удачное чтение регистра на устройстве

                        // узнаем длину данных
                        int dataLen = BitConverter.ToInt16(receiveBuffer, startIndex + 19);
                        sjNumber = BitConverter.ToInt32(receiveBuffer, startIndex + 21);
                        log.InfoFormat("get reading register #314: sjNumber = {0}, deviceid = {1}, data size = {2}",
                            sjNumber, deviceId, dataLen);

                        
                    }
                    else
                    {
                        // не удачное чтение регистра
                        log.WarnFormat("error reading register #314: {0}, deviceid = {1}",
                            receiveBuffer[startIndex + 13].ToString("X2"), deviceId);
                    }
                }
            }

            return sjNumber - 1;
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
                }
            }
            catch (Exception ex)
            {
                log.Error(ex);
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