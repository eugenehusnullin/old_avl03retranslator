using log4net;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using TcpServer.Core.async.common;
using TcpServer.Core.async.othertasks.model;
using TcpServer.Core.Properties;

namespace TcpServer.Core.async.othertasks
{
    public class FediaTask
    {
        private ILog log;
        private HashSet<string> imeiSet;
        private bool enabled;
        private string datetimeFormat = "dd.MM.yyyy HH:mm:ss";

        public FediaTask()
        {
            log = LogManager.GetLogger(typeof(FediaTask));
            enabled = Settings.Default.Fedia_Enabled;
            if (enabled)
            {
                var imeiListFilename = Settings.Default.Fedia_ImeiListFileName;
                imeiSet = ImeiListLoader.loadImeis(log, imeiListFilename);
            }
        }

        public void task(string imei, string text, BasePacket packet)
        {
            if (enabled && imeiSet.Contains(imei))
            {
                maindata rowData = new maindata();
                rowData.date = DateTime.Now;
                rowData.text = text;
                rowData.imei = imei;
                rowData.lat = packet.Latitude.ToString();
                rowData.lon = packet.Longitude.ToString();
                rowData.gpsdate = packet.RTC.ToString(datetimeFormat);
                rowData.speed = packet.Speed.ToString();

                using (var db = new FediaEntities())
                {
                    db.maindatas.AddObject(rowData);
                    db.SaveChanges();
                }
            }
        }
    }
}
