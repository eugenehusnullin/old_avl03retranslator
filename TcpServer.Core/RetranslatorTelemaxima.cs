using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Net;
using System.Net.Sockets;
using System.Threading;
using System.Web.Services.Protocols;
using MySql.Data.MySqlClient;
using System.Data;

namespace TcpServer.Core
{
    public class RetranslatorTelemaxima
    {
        public RetranslatorTelemaxima(string srcIpAddress, int srcPort) : this(srcIpAddress, srcPort, null, new Options { LogPath = "RetranslatorTelemaximaLog" }) { }
        public RetranslatorTelemaxima(string srcIpAddress, int srcPort, EventLog eventLog, Options options)
        {
            SrcHost = srcIpAddress;
            SrcPort = srcPort;

            Options = options;
            Logger = new Logger(eventLog, options.LogPath);
        }

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
        public MySqlConnection cnn;//для чтения id  из бд
        public DataSet ds;
        public MySqlDataAdapter adap;
        public MySqlCommandBuilder bild;
        public MySqlCommand selectcom = new MySqlCommand();
        public string selecttext;
        public string tablename;//для чтения id  из бд

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
            //

            //
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

                       //var basePacket = BasePacket.GetFromWialon(buffer);
                        var basePacket = BasePacket.GetFromGlonass(packet);//возможно что gps
                        currentImei = basePacket.IMEI;//id блока
                        Logger.PacketWriteLine(currentImei + " " + packet);

                        //добавить поиск в бд
                        cnn = new MySqlConnection("Database = telemax; Data Source = localhost; User Id = root; Password = 1341");//;New=True;Version=2
                        selecttext = "select * from crewnum where idcrew=" + basePacket.IMEI;
                        adap = new MySqlDataAdapter();
                        adap.SelectCommand = selectcom;
                        selectcom.Connection = cnn;
                        selectcom.CommandText = selecttext;
                        ds.Tables.Add(tablename);
                        adap.FillLoadOption = LoadOption.OverwriteChanges;                        
                        adap.Fill(ds, tablename);
                        //поиск в бд


                        var wsdl = new TelemetryService.TelemetryService();
                        wsdl.UseDefaultCredentials = true;

                        var telemetry = new TelemetryService.telemetryBa();
                        telemetry.gpsCode = basePacket.IMEI;
                        telemetry.coordX = basePacket.Longitude;
                        telemetry.coordY = basePacket.Latitude;
                        telemetry.date = basePacket.RTC.AddHours(4);
                        telemetry.speed = basePacket.Speed;
                        telemetry.glonass = false;

                        var telemetryDetailsCollection = new List<TelemetryService.telemetryDetailBa>();
                        var telemetryDetails = new TelemetryService.telemetryDetailBa();
                        telemetryDetails.sensorCode = "pwr_ext";
                        telemetryDetails.value = 12;
                        telemetryDetailsCollection.Add(telemetryDetails);

                        wsdl.storeTelemetry(telemetry, telemetryDetailsCollection.ToArray());

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

namespace TcpServer.Core.TelemaximaService
{
    public partial class TelemaximaService
    {
       /* protected override System.Xml.XmlWriter GetWriterForMessage(SoapClientMessage message, int bufferSize)
        {

            /*var header = new SoapHeaderSecurity();
            header.MustUnderstand = true;
            header.UsernameToken.Username = "braitmonitor";
            header.UsernameToken.Password = "cup6ztd3BvOd";
            //header.UsernameToken.Username = "braitmonitor_test";
            //header.UsernameToken.Password = "braitmonitor123";
            message.Headers.Add(header);
            return base.GetWriterForMessage(message, bufferSize);*/
       /*}*/
    }

    [System.Xml.Serialization.XmlRootAttribute("Security", Namespace = "http://docs.oasis-open.org/wss/2004/01/oasis-200401-wss-wssecurity-secext-1.0.xsd", DataType = "wsse", IsNullable = false)]
    public class SoapHeaderSecurity : SoapHeader
    {
        public SoapHeaderSecurity()
        {
            UsernameToken = new UsernameToken();
        }
        public UsernameToken UsernameToken { get; set; }
    }

    public class UsernameToken
    {
        public string Username { get; set; }
        public string Password { get; set; }
    }
}