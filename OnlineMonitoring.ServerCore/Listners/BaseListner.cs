using System;
using System.Diagnostics;
using System.IO;
using System.Net;
using System.Net.Sockets;
using System.Text;
using System.Threading;
using OnlineMonitoring.ServerCore.DataBase;

namespace OnlineMonitoring.ServerCore.Listners
{
    public abstract class BaseListner
    {
        public string SrcHost { get; private set; }
        public int SrcPort { get; private set; }

        private Options Options { get; set; }
        protected Logger Logger { get; set; }

        public ServiceState State { get; private set; }
        public enum ServiceState
        {
            Starting, Started, Stoping, Stoped
        }

        public Thread CurrentThread { get; private set; }
        
        protected BaseListner(string srcIpAddress, int srcPort, EventLog eventLog, Options options)
        {
            SrcHost = srcIpAddress;
            SrcPort = srcPort;
            Options = options;
            Logger = new Logger(eventLog, options.LogPath);
        }

        public void Start()
        {
            State = ServiceState.Starting;
            CurrentThread = new Thread(DoWork);
            CurrentThread.Start();
        }
        public void Stop()
        {
            State = ServiceState.Stoping;
            CurrentThread.Abort();
        }

        private void DoWork()
        {
            TcpListener tcpListener = null;
            try
            {
                var localAddr = IPAddress.Parse(SrcHost);
                tcpListener = new TcpListener(localAddr, SrcPort);

                tcpListener.Start();

                while (State != ServiceState.Stoping)
                {
                    var acceptTcpClient = tcpListener.AcceptTcpClient();
                    var thread = new Thread(() => DoProcess(acceptTcpClient));
                    thread.Start();
                }
            }
            catch (Exception ex)
            {
                Logger.ErrorWriteLine(ex);
            }
            if (tcpListener != null) tcpListener.Stop();
        }

        protected void DoProcess(Object clientObject)
        {
            TcpClient client = null;
            NetworkStream stream = null;

            try
            {
                client = (TcpClient)clientObject;
                stream = client.GetStream();

                var db = new DataBaseManager(Options.ConnectionString);

                StreamProcessing(stream, db);
            }
            catch (IOException ex)
            {
                Logger.WarningWriteLine(ex);
            }
            catch (Exception ex)
            {
                Logger.ErrorWriteLine(ex);
            }
            if (stream != null) stream.Close();
            if (client != null) client.Close();
        }

        protected abstract void StreamProcessing(NetworkStream stream, DataBaseManager dataBaseManager);
    }
}