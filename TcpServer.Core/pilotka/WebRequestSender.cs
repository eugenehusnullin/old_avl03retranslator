using Apache.NMS;
using Apache.NMS.Util;
using log4net;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Text;
using System.Threading.Tasks;
using System.Web;
using TcpServer.Core.Properties;

namespace TcpServer.Core.pilotka
{
    public class WebRequestSender
    {
        private PilotkaSettings settings;
        private string url;
        private ILog log;
        private string datetimeFormat = "dd.MM.yyyy HH:mm:ss";
        private int webRequestTimeout = 10000;

        private ISession session = null;
        private IMessageProducer producer = null;
        private IConnection connection = null;

        public WebRequestSender(PilotkaSettings settings)
        {
            this.settings = settings;
            this.log = LogManager.GetLogger(settings.LoggerName);
            this.url = settings.Url;
        }

        public async Task<bool> send(string imei, PilotkaState state, DateTime utcDatetime)
        {
            return await Task<bool>.Run(() =>
                {
                    lock (this)
                    {
                        string currentUrl = url;
                        currentUrl = currentUrl.Replace("{IMEI}", imei)
                            .Replace("{STATE}", state == PilotkaState.Started ? "1" : "0")
                            .Replace("{UTC}", HttpUtility.UrlEncode(utcDatetime.ToString(datetimeFormat)));

                        HttpWebRequest webRequest = (HttpWebRequest)WebRequest.Create(currentUrl);
                        webRequest.Timeout = webRequestTimeout;
                        webRequest.Method = "GET";
                        try
                        {
                            var webResponse = (HttpWebResponse)webRequest.GetResponse();
                            log.DebugFormat("WebRequestSender: webResponse = {0}, url={1}", webResponse.StatusCode, currentUrl);

                            // active mq post message
                            if (Settings.Default.Activemq_enabled)
                            {
                                nsmSend(imei, state, utcDatetime);
                            }

                            return webResponse.StatusCode == HttpStatusCode.OK;
                        }
                        catch (Exception e)
                        {
                            log.Error("WebRequestSender: " + currentUrl + ": " + e.ToString());
                            return false;
                        }
                        finally
                        {
                            webRequest.GetResponse().Close();
                            webRequest.GetResponse().Dispose();
                        }
                    }
                }
            );
        }

        private void createConnection()
        {
            Uri connectUri = new Uri(Settings.Default.Activemq_url);
            var factory = new NMSConnectionFactory(connectUri);
            connection = factory.CreateConnection(Settings.Default.Activemq_user, Settings.Default.Activemq_password);
            connection.Start();
            session = connection.CreateSession(AcknowledgementMode.AutoAcknowledge);
            var queue = SessionUtil.GetQueue(session, Settings.Default.Activemq_queue);
            producer = session.CreateProducer(queue);
            producer.DeliveryMode = MsgDeliveryMode.Persistent;
        }

        private void closeConnection()
        {
            try
            {
                producer.Close();
            }
            catch { }

            try
            {
                session.Close();
            }
            catch { }

            try
            {
                connection.Stop();
            }
            catch { }

            try
            {
                connection.Close();
            }
            catch { }

            producer = null;
            session = null;
            connection = null;
        }

        private bool nsmSend(string imei, PilotkaState state, DateTime utcDatetime)
        {
            try
            {
                if (session == null)
                {
                    createConnection();
                }

                string msg = imei + "|" + (state == PilotkaState.Started ? "1" : "0") + "|" + utcDatetime.ToString(datetimeFormat);
                var textMessage = session.CreateTextMessage(msg);
                producer.Send(textMessage);
                return true;
            } catch {
                closeConnection();
                return false;
            }
        }
    }
}
