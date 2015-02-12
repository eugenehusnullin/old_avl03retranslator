using log4net;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.Web;
using System.Xml;
using TcpServer.Core.async.common;
using TcpServer.Core.Properties;

namespace TcpServer.Core.gis
{
    public class GISHandler
    {
        private ILog log;
        private object locker = new object();
        private Dictionary<string, List<BasePacket>> dictionary = new Dictionary<string, List<BasePacket>>();
        private DateTime min = DateTime.MaxValue;
        private readonly TimeSpan allowTimeSpan = new TimeSpan(0, 4, 30);
        private HashSet<String> imeis = new HashSet<string>();
        private bool allImeis = false;
        private volatile bool stoped = false;
        private Thread thread;

        public void start()
        {
            log = LogManager.GetLogger(typeof(GISHandler));

            if (!Settings.Default.GIS_Enabled)
            {
                return;
            }

            allImeis = Settings.Default.GIS_Allboards;
            if (!allImeis)
            {
                imeis = ImeiListLoader.loadImeis(log, Settings.Default.GIS_ImeiListFileName);
            }

            stoped = false;
            thread = new Thread(() => process());
            thread.Start();
            log.Info("2GIS started");
        }

        public void stop()
        {
            if (!Settings.Default.GIS_Enabled)
            {
                return;
            }

            stoped = true;
            thread.Interrupt();
            thread.Join();
            log.Info("2GIS stoped");
        }

        public void handle(BasePacket basePacket)
        {
            try
            {
                if (!Settings.Default.GIS_Enabled)
                {
                    return;
                }

                if (!allImeis && !imeis.Contains(basePacket.IMEI))
                {
                    return;
                }

                TimeSpan ts = DateTime.UtcNow.Subtract(basePacket.RTC);
                if (ts <= this.allowTimeSpan)
                {
                    lock (this.locker)
                    {
                        if (!this.dictionary.ContainsKey(basePacket.IMEI))
                        {
                            this.dictionary.Add(basePacket.IMEI, new List<BasePacket>());
                        }
                        var list = this.dictionary[basePacket.IMEI];
                        list.Add(basePacket);

                        if (basePacket.RTC < this.min)
                        {
                            this.min = basePacket.RTC;
                        }
                    }
                }
            }
            catch (Exception e)
            {
                log.Error(e.ToString());
            }
        }

        private void process()
        {
            try
            {
                while (!stoped)
                {
                    Dictionary<string, List<BasePacket>> sendList = null;
                    lock (this.locker)
                    {
                        TimeSpan ts = DateTime.UtcNow.Subtract(this.min);
                        if (ts >= this.allowTimeSpan)
                        {
                            sendList = this.dictionary;
                            this.dictionary = new Dictionary<string, List<BasePacket>>();
                            this.min = DateTime.MaxValue;
                        }
                    }

                    if (sendList != null && sendList.Count > 0)
                    {
                        send2GIS(sendList);
                    }
                    else
                    {
                        Thread.Sleep(3000);
                    }
                }
            }
            catch (Exception e)
            {
                log.Error(e.ToString());
            }
        }

        private bool send2GIS(Dictionary<string, List<BasePacket>> sendList)
        {
            try
            {
                var xmlDoc = buildXml(sendList);
                var values = xmlDoc.OuterXml;

                log.Debug(values);

                var webRequest = (HttpWebRequest)WebRequest.Create(Settings.Default.GIS_Url);
                webRequest.Timeout = 10000;
                webRequest.Method = "POST";
                webRequest.ContentType = "application/x-www-form-urlencoded";
                using (var writer = new StreamWriter(webRequest.GetRequestStream()))
                {
                    writer.Write("data=" + HttpUtility.UrlEncode(values, Encoding.UTF8));
                }
                var webResponse = (HttpWebResponse)webRequest.GetResponse();
                return webResponse.StatusCode != HttpStatusCode.OK;
            }
            catch (Exception e)
            {
                log.Error(e.ToString());
                return false;
            }
        }

        private XmlDocument buildXml(Dictionary<string, List<BasePacket>> sendList)
        {
            XmlDocument xmlDoc = new XmlDocument();
            XmlNode rootNode = xmlDoc.CreateElement("tracks");
            XmlAttribute attribute = xmlDoc.CreateAttribute("clid");
            attribute.Value = Settings.Default.GIS_CLID;
            rootNode.Attributes.Append(attribute);
            xmlDoc.AppendChild(rootNode);

            foreach (var pair in sendList)
            {
                XmlNode trackNode = xmlDoc.CreateElement("track");
                attribute = xmlDoc.CreateAttribute("uuid");
                attribute.Value = pair.Key;
                trackNode.Attributes.Append(attribute);
                rootNode.AppendChild(trackNode);

                foreach (var item in pair.Value)
                {
                    XmlNode pointNode = xmlDoc.CreateElement("point");

                    attribute = xmlDoc.CreateAttribute("latitude");
                    attribute.Value = Convert.ToString(item.Latitude);
                    pointNode.Attributes.Append(attribute);

                    attribute = xmlDoc.CreateAttribute("longitude");
                    attribute.Value = Convert.ToString(item.Longitude);
                    pointNode.Attributes.Append(attribute);

                    attribute = xmlDoc.CreateAttribute("avg_speed");
                    attribute.Value = Convert.ToString(Convert.ToInt32(item.Speed));
                    pointNode.Attributes.Append(attribute);

                    attribute = xmlDoc.CreateAttribute("direction");
                    attribute.Value = Convert.ToString(Convert.ToInt32(item.Direction));
                    pointNode.Attributes.Append(attribute);

                    attribute = xmlDoc.CreateAttribute("time");
                    attribute.Value = item.RTC.ToString("ddMMyyyy:HHmmss");
                    pointNode.Attributes.Append(attribute);

                    trackNode.AppendChild(pointNode);
                }
            }

            return xmlDoc;
        }
    }
}
