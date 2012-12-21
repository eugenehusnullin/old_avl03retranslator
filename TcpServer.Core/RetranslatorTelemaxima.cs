using System;
using System.Collections.Generic;
using System.Collections.Concurrent;
using System.IO;
using System.Linq;
using System.Net;
using System.Net.Sockets;
using System.Text;
using System.Xml;
using System.Data;
using System.Data.OleDb;
using System.Threading;
using log4net;

namespace TcpServer.Core
{
    public class RetranslatorTelemaxima
    {
        private Dictionary<String, Int32> carsIDs;
        private volatile ConcurrentQueue<BasePacket> packets;
        private ILog log;
        private int workersCount = 5;
        private List<Thread> workersList;
        private int attemptsRetranslate = 3;
        private int sleepTimeout = 4000;
        private volatile bool stoped = false;
        private int webRequestTimeout = 10000;

        public RetranslatorTelemaxima()
        {
            log = LogManager.GetLogger(typeof(RetranslatorTelemaxima));
            carsIDs = new Dictionary<String, Int32>();
            packets = new ConcurrentQueue<BasePacket>();
            workersList = new List<Thread>();
        }

        public void start()
        {
            try
            {
                string servicePath = Path.GetDirectoryName(System.Reflection.Assembly.GetEntryAssembly().Location);
                string mdbPath = Path.Combine(servicePath, "maxima_taxi.mdb");
                OleDbConnection con = new OleDbConnection("Provider=Microsoft.Jet.OLEDB.4.0; Data Source=" + mdbPath + ";");
                con.Open();
                OleDbCommand select = new OleDbCommand("select imei,id from main", con);
                OleDbDataReader reader = select.ExecuteReader();
                int carsCount = 0;
                while (reader.Read())
                {
                    carsCount++;
                    carsIDs.Add(reader[0].ToString(), int.Parse(reader[1].ToString()));
                }
                con.Close();

                log.InfoFormat("RetranslatorTelemaxima starting. Loaded {0} carIDs.", carsCount);

                for (int i = 0; i < workersCount; i++)
                {
                    Thread worker = new Thread(() => process());
                    worker.Start();
                    workersList.Add(worker);
                }

                log.Info("RetranslatorTelemaxima started.");
            }
            catch (Exception e)
            {
                log.Error("Error in constructor RetranslatorTelemaxima", e);
                throw e;
            }
        }

        public void stop()
        {
            stoped = true;

            foreach (Thread worker in workersList)
            {
                worker.Interrupt();
            }

            foreach (Thread worker in workersList)
            {
                worker.Join();
            }

            log.Info("RetranslatorTelemaxima stoped.");
        }

        public void checkAndRetranslate(BasePacket packet)
        {
            if (carsIDs.ContainsKey(packet.IMEI))
            {
                packets.Enqueue(packet);
            }
        }

        private bool retranslate(int carID, double lat, double lon)
        {
            try
            {
                var normLat = lat.ToString().Replace(".", ",");
                var normLon = lon.ToString().Replace(".", ",");
                HttpWebRequest webRequest = (HttpWebRequest)WebRequest.Create(
                    String.Format("http://93.191.61.125:27000//??type=set_car_gps&id_car={0}&lat={1}&lon={2}", carID, normLat, normLon));
                webRequest.Timeout = webRequestTimeout;
                webRequest.Method = "GET";
                try
                {
                    var webResponse = (HttpWebResponse)webRequest.GetResponse();
                    string stringResponse = new StreamReader(webResponse.GetResponseStream(), Encoding.Default).ReadToEnd();
                    bool retranslated = stringResponse.Equals("OK");

                    if (retranslated)
                    {
                        log.InfoFormat("Retranlated carID = {0}, geo = {1}, {2}", carID, normLat, normLon);
                    }
                    else
                    {
                        log.ErrorFormat("Not retranslated carID = {0}, geo = {1}, {2} - error is: {3}", carID, normLat, normLon, stringResponse);
                    }
                    return retranslated;
                }
                finally
                {
                    webRequest.GetResponse().Close();
                }
            }
            catch (Exception e)
            {
                log.Error(String.Format("Error retranslate telemaxima carID = {0}, geo = {1}, {2}", carID, lat, lon), e);
            }
            return false;
        }

        private void process()
        {
            while (!stoped)
            {
                BasePacket packet;
                if (packets.TryDequeue(out packet))
                {
                    int carID;
                    if (carsIDs.TryGetValue(packet.IMEI, out carID))
                    {
                        int attempt = 0;
                        while (attempt < attemptsRetranslate)
                        {
                            if (retranslate(carID, packet.Latitude / 100, packet.Longitude / 100))
                            {
                                break;
                            }
                            attempt++;
                        }
                    }
                }
                else
                {
                    try
                    {
                        Thread.Sleep(sleepTimeout);
                    }
                    catch
                    {
                    }
                }
            }
        }
    }
}
