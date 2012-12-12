using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net;
using System.Net.Sockets;
using System.Text;
using System.Xml;
using System.Data;
using System.Data.OleDb;
using System.Threading;

namespace TcpServer.Core
{

    public class RetranslatorTelemaxima
    {
        private static Dictionary<String, Int32> carsIDs = new Dictionary<String, Int32>();
        private static Logger logger;
        private static Logger mainLogger;

        public static void Init(Logger mainLogger)
        {
            RetranslatorTelemaxima.mainLogger = mainLogger;

            try
            {
                string servicePath = Path.GetDirectoryName(System.Reflection.Assembly.GetEntryAssembly().Location);
                RetranslatorTelemaxima.logger = new Logger(null, servicePath, "telemaxima");

                string mdbPath = Path.Combine(servicePath, "maxima_taxi.mdb");
                OleDbConnection con = new OleDbConnection("Provider=Microsoft.Jet.OLEDB.4.0; Data Source=" + mdbPath + ";");
                con.Open();
                OleDbCommand select = new OleDbCommand("select imei,id from main", con);
                OleDbDataReader reader = select.ExecuteReader();
                int i = 0;
                while (reader.Read())
                {
                    i++;
                    carsIDs.Add(reader[0].ToString(), int.Parse(reader[1].ToString()));
                }
                con.Close();

                logger.MessageWriteLine(String.Format("Init complete. Loaded {0} carIDs.", i));
            }
            catch (Exception e)
            {
                mainLogger.ErrorWriteLine(e);
            }
        }

        public static bool needRetranslate(string IMEI)
        {
            return carsIDs.ContainsKey(IMEI);
        }

        private static bool Retranslate_To_Maxima(string ip, int port, int car_id, double lat, double lon)
        {
            try
            {
                //1 на линии 2 на заказе 3 перерыв 4 не работает
                var real_lat = lat.ToString().Replace(".", ",");
                var real_lon = lon.ToString().Replace(".", ",");
                var Send_Query = (HttpWebRequest)WebRequest.Create("http://" + ip + ":" + port + "//??type=set_car_gps&id_car=" + car_id.ToString() + "&lat=" + real_lat + "&lon=" + real_lon);
                Send_Query.Timeout = 10000;
                Send_Query.Method = "GET";
                var Data = (HttpWebResponse)Send_Query.GetResponse();
                string HTML = new StreamReader(Data.GetResponseStream(), Encoding.Default).ReadToEnd();
                logger.MessageWriteLine(String.Format("MAXIMA: {0}", "Answer on Maxima's retranslation try is: " + HTML));
                Send_Query.GetResponse().Close();
                Data.GetResponseStream().Close();
                if (HTML == "OK")
                    return true;
                else return false;
            }
            catch (Exception ex)
            {
                mainLogger.ErrorWriteLine(ex);
                return false;
            }
        }
        
        private static int Get_Crew_State(string ip, int port, int car_id, string good_states)
        {
            try
            {
                var Send_Query = (HttpWebRequest)WebRequest.Create("http://" + ip + ":" + port + "//??type=get_car_info&id_car=" + car_id);
                Send_Query.Timeout = 10000;
                Send_Query.Method = "GET";
                var Data = (HttpWebResponse)Send_Query.GetResponse();
                var html = new StreamReader(Data.GetResponseStream(), Encoding.Default).ReadToEnd();
                var Doc = new XmlDocument();
                Doc.LoadXml(html);
                var state = Doc.GetElementsByTagName("id_crew_state")[0].InnerText;
                logger.MessageWriteLine(String.Format("MAXIMA: {0}", "Got crew info successfully. Crew state is: " + state));
                Send_Query.GetResponse().Close();
                Data.GetResponseStream().Close();
                //1 на линии 2 на заказе 3 перерыв 4 не работает
                //Если строка good_states содержит подстроку state
                //То запрос считается удовлетворительным.
                //Строка good_states имеет вид X,X,X,X где Х - одно из состояний от 1 до 4
                if (good_states.Contains(state))
                {
                    return 1;
                }
                // Состояние 0 - запрос прошел успешно, но машина не свободна
                else
                {
                    logger.MessageWriteLine(String.Format("MAXIMA: {0}", "There is no need to retranslate that crew (" + car_id + "). It is not free."));
                    return 0;
                }
            }
            catch (Exception ex)
            {
                mainLogger.ErrorWriteLine(ex);
                return -1;
            }
        }

        public static void DoMaxima(object stateInfo)
        {
            var Packet = (BasePacket)stateInfo;

            int max_try = 3;
            int crew_state = -1;
            bool retranslated = false;
            int car_id = -1;

            if (carsIDs.TryGetValue(Packet.IMEI, out car_id))
            {
                //1 на линии 2 на заказе 3 перерыв 4 не работает
                do
                {
                    logger.MessageWriteLine(String.Format("MAXIMA: {0}", "Try to get crew state in Maxima API. Crew ID: " + car_id));
                    crew_state = Get_Crew_State("93.191.61.125", 27000, car_id, "1,2,3");
                    if (crew_state > -1)
                    {
                        break;
                    }
                    max_try--;
                }
                while (max_try > 0);
                max_try = 3;
                if (crew_state == 1)
                {
                    do
                    {
                        logger.MessageWriteLine(String.Format("MAXIMA: {0}", "Try to set coordinates in Maxima API. Lon: " + Packet.Longitude / 100 + " Lat: " + Packet.Latitude / 100));
                        retranslated = Retranslate_To_Maxima("93.191.61.125", 27000, car_id, Packet.Latitude / 100, Packet.Longitude / 100);
                        if (retranslated)
                        {
                            logger.MessageWriteLine(String.Format("MAXIMA: {0}", "Packet successfully RETRANSLATED. IMEI: " + Packet.IMEI));
                            break;
                        }
                    }
                    while (max_try > 0);
                }

                if (car_id != -1 && !retranslated && crew_state == 1)
                    logger.MessageWriteLine(String.Format("MAXIMA: {0}", Environment.NewLine, "Packet IS NOT RETRANSLATED, BUT SHOULD BE."));
            }
        }
    }
}
