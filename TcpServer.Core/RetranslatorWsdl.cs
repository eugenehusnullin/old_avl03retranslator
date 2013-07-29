using Microsoft.Web.Services3.Design;
using Microsoft.Web.Services3.Security.Tokens;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Net;
using System.Net.Sockets;
using System.Threading;
using System.Web.Services.Protocols;
using TestOds;

namespace TcpServer.Core
{
    public class RetranslatorWsdl
    {
        public RetranslatorWsdl(string srcIpAddress, int srcPort) : this(srcIpAddress, srcPort, null, new Options { LogPath = "RetranslatorWsdlLog" }, null, null) { }
        public RetranslatorWsdl(string srcIpAddress, int srcPort, EventLog eventLog, Options options, string username, string password)
        {
            SrcHost = srcIpAddress;
            SrcPort = srcPort;

            Options = options;
            Logger = new Logger(eventLog, options.LogPath);

            this.username = username;
            this.password = password;
        }

        private string username;
        private string password;

        public Options Options { get; set; }
        public Logger Logger { get; set; }

        public string SrcHost { get; private set; }
        public int SrcPort { get; private set; }

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

            var currentImei = string.Empty;

            try
            {
                while (State != ServiceState.Stoping)
                {
                    blockClient = (TcpClient)blockClientObject;
                    blockStream = blockClient.GetStream();

                    if (!blockStream.CanRead) throw new Exception("Невозможно получить данные от блока");

                    // Buffer for reading data lenght
                    var bytes = new Byte[4];
                    int i;
                    lock (_thisLock)
                    {
                        i = blockStream.Read(bytes, 0, bytes.Length);
                    }

                    if (i > 0)
                    {
                        var bufferSize = BitConverter.ToInt32(bytes, 0);

                        var buffer = new Byte[bufferSize];
                        lock (_thisLock)
                        {
                            blockStream.Read(buffer, 0, buffer.Length);
                        }

                        var packet = ByteHelper.GetStringFromBytes(buffer);

                        var basePacket = BasePacket.GetFromWialon(buffer);

                        currentImei = basePacket.IMEI;
                        Logger.PacketWriteLine(currentImei + " " + packet);
                        
                        PolicyAssertion[] policyAssertion = new PolicyAssertion[] {
                            new OdsTransportAssertion()
                            //new UsernameOverTransportAssertion()
                        };
                        Policy policy = new Policy(policyAssertion);

                        TelemetryService service = new TelemetryService();
                        service.SetPolicy(policy);
                        service.SetClientCredential(new UsernameToken(username, password, PasswordOption.SendPlainText));

                        var telemetry = new telemetryBa();
                        telemetry.gpsCode = basePacket.IMEI;
                        telemetry.coordX = basePacket.Longitude;
                        telemetry.coordY = basePacket.Latitude;
                        telemetry.date = basePacket.RTC.AddHours(4);
                        telemetry.speed = basePacket.Speed;
                        telemetry.glonass = false;

                        var telemetryDetailsCollection = new List<telemetryDetailBa>();
                        var telemetryDetails = new telemetryDetailBa();
                        telemetryDetails.sensorCode = "pwr_ext";
                        telemetryDetails.value = 12;
                        telemetryDetailsCollection.Add(telemetryDetails);

                        service.storeTelemetry(telemetry, telemetryDetailsCollection.ToArray());

                        var resultBuffer = new[] { (byte)0x11 };
                        lock (_thisLock)
                        {
                            blockStream.Write(resultBuffer, 0, resultBuffer.Length);
                        }
                    }
                    Thread.Sleep(50);
                }
            }
            catch (IOException ex)
            {
                Logger.WarningWriteLine(currentImei + ex);
            }
            catch (SoapException ex)
            {
                Logger.WarningWriteLine(currentImei + ex);
            }
            catch (WebException ex)
            {
                Logger.ErrorWriteLine(currentImei + ex);
            }
            catch (Exception ex)
            {
                Logger.ErrorWriteLine(currentImei + ex);
            }

            if (blockStream != null) blockStream.Close();
            if (blockClient != null) blockClient.Close();
        }
    }
}