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
        private static Dictionary<string, int> DB = new Dictionary<string, int>();

        static RetranslatorTelemaxima()
        {
            OleDbConnection con = new OleDbConnection("Provider=Microsoft.Jet.OLEDB.4.0; Data Source=maxima_taxi.mdb;");

            OleDbCommand select = new OleDbCommand("select imei,id from main", con);
            OleDbDataReader reader = select.ExecuteReader();
            while (reader.Read())
            {
                DB.Add(reader[0].ToString(), int.Parse(reader[1].ToString()));
            }
            con.Close();            
        }
        private static bool Retranslate_To_Maxima(string ip, int port, int car_id, double lat, double lon)
        {
            StringBuilder log = new StringBuilder();
            try
            {
                //1 на линии 2 на заказе 3 перерыв 4 не работает
                var real_lat = lat.ToString().Replace(".", ",");
                var real_lon = lon.ToString().Replace(".", ",");
                var Send_Query = (HttpWebRequest)WebRequest.Create("http://" + ip + ":" + port + "//??type=set_car_gps&id_car=" + car_id.ToString() + "&lat=" + real_lat + "&lon=" + real_lon);
                Send_Query.Timeout = 10000;
                var Data = (HttpWebResponse)Send_Query.GetResponse();
                string HTML = new StreamReader(Data.GetResponseStream(), Encoding.Default).ReadToEnd();
                log.AppendFormat("{0}{1} MAXIMA: {2}", Environment.NewLine, DateTime.Now.ToString(), "Answer on Maxima's retranslation try is: " + HTML);
                if (HTML == "OK")
                    return true;
                else return false;
            }
            catch (Exception ex)
            {
                log.AppendFormat("{0}{1} MAXIMA: {2}", Environment.NewLine, DateTime.Now.ToString(), "Retranslation faild. Reason: " + ex.Message);
                return false;
            }
        }
        private static int Get_Crew_State(string ip, int port, int car_id, string good_states)
        {
            StringBuilder log = new StringBuilder();
            try
            {
                var Send_Query = (HttpWebRequest)WebRequest.Create("http://" + ip + ":" + port + "//??type=get_car_info&id_car=" + car_id);
                Send_Query.Timeout = 10000;
                var Data = (HttpWebResponse)Send_Query.GetResponse();
                var html = new StreamReader(Data.GetResponseStream(), Encoding.Default).ReadToEnd();
                var Doc = new XmlDocument();
                Doc.LoadXml(html);
                var state = Doc.GetElementsByTagName("id_crew_state")[0].InnerText;
                //1 на линии 2 на заказе 3 перерыв 4 не работает
                //Если строка good_states содержит подстроку state
                //То запрос считается удовлетворительным.
                //Строка good_states имеет вид X,X,X,X где Х - одно из состояний от 1 до 4
                if (good_states.Contains(state))
                {
                    //Состояние 1 - запрос прошел успешно и машина свободна
                    log.AppendFormat("{0}{1} MAXIMA: {2}", Environment.NewLine, DateTime.Now.ToString(), "Gor crew info successfully. Crew state is: " + state);
                    return 1;
                }
                // Состояние 0 - запрос прошел успешно, но машина не свободна
                else return 0;
            }
            catch (Exception ex)
            {
                // Состояние -1 будет означать, что запрос не удался и его необходимо повторить.
                log.AppendFormat("{0}{1} MAXIMA: {2}", Environment.NewLine, DateTime.Now.ToString(), "Error while trying to get crew state. Reason: " + ex.Message);
                return -1;
            }
        }
        /*private static int Get_ID_From_DB(string imei)
        {
                StringBuilder log = new StringBuilder();
                int id = 0;
                OleDbConnection con = new OleDbConnection("Provider=Microsoft.Jet.OLEDB.4.0; Data Source=maxima_taxi.mdb;");
                if (con.State != ConnectionState.Open)
                    con.Open();
                log.AppendFormat("{0}{1} MAXIMA: {2}",Environment.NewLine, DateTime.Now.ToString()  ,"Database connected successfully.");
                OleDbCommand select_id = new OleDbCommand("select id from main where imei='" + imei + "'", con);
                OleDbDataReader reader = select_id.ExecuteReader();
                reader.Read();
                if (reader.HasRows)
                {
                    id = int.Parse(reader[0].ToString());
                    log.AppendFormat("{0}{1} MAXIMA: {2}", Environment.NewLine, DateTime.Now.ToString(), "Got a car ID for retranslate: " + id.ToString());
                }
                else 
                { 
                    id = 0;
                    log.AppendFormat("{0}{1} MAXIMA: {2}", Environment.NewLine, DateTime.Now.ToString(), "There is no car wiht IMEI: " + imei + " in database.");
                }
                reader.Close();
                con.Close();
                log.AppendFormat("{0}{1} MAXIMA: {2}", Environment.NewLine, DateTime.Now.ToString(), "Database connection closed successfully.");
                return id;
        }
         * */
        public static bool DoMaxima(BasePacket Packet)
        {
            StringBuilder log = new StringBuilder();
            int max_try = 3;
            int crew_state = -1;
            bool retranslated = false;
            int car_id = 0;
            if (DB.TryGetValue(Packet.IMEI, out car_id))
            {
                //1 на линии 2 на заказе 3 перерыв 4 не работает
                do
                {
                    log.AppendFormat("{0}{1} MAXIMA: {2}", Environment.NewLine, DateTime.Now.ToString(), "Try to get crew state in Maxima API.");
                    crew_state = Get_Crew_State("93.191.61.125", 27000, car_id, "1");
                    if (crew_state > -1)
                        break;
                    max_try--;
                }
                while (max_try > 0);
                max_try = 3;
                if (crew_state == 1)
                {
                    do
                    {
                        log.AppendFormat("{0}{1} MAXIMA: {2}", Environment.NewLine, DateTime.Now.ToString(), "Try to set coordinates in Maxima API. Lon: " + Packet.Longitude / 100 + " Lat: " + Packet.Latitude / 100);
                        retranslated = Retranslate_To_Maxima("93.191.61.125", 27000, car_id, Packet.Latitude / 100, Packet.Longitude / 100);
                        if (retranslated)
                        {
                            break;
                        }
                    }
                    while (max_try > 0);
                }
            }
            else
            {
                log.AppendFormat("{0}{1} MAXIMA: {2}", Environment.NewLine, DateTime.Now.ToString(), "There is no need to retranslate packet.");
            }
            if (retranslated)
            {
                log.AppendFormat("{0}{1} MAXIMA: {2}", Environment.NewLine, DateTime.Now.ToString(), "Packet successfully RETRANSLATED.");
            }
            else
            {
                if (car_id != 0)
                    log.AppendFormat("{0}{1} MAXIMA: {2}", Environment.NewLine, DateTime.Now.ToString(), "Packet IS NOT RETRANSLATED, BUT SHOULD BE.");
            }
            File.AppendAllText("MaximaRetranslation.txt", log.ToString(), Encoding.Default);
            return retranslated;
        }
    }
}
