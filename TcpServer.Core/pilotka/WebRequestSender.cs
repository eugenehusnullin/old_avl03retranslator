using log4net;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Text;
using System.Threading.Tasks;

namespace TcpServer.Core.pilotka
{
    public class WebRequestSender
    {
        private PilotkaSettings settings;
        private string url;
        private ILog log;
        private string datetimeFormat = "yyyy-MM-dd-HH-mm-ss";
        private int webRequestTimeout = 10000;

        public WebRequestSender(PilotkaSettings settings)
        {
            this.settings = settings;
            this.log = LogManager.GetLogger(settings.LoggerName);
            this.url = settings.Url;
        }

        public async Task<bool> send(string imei, EngineState engineState, DateTime utcDatetime)
        {
            return await Task<bool>.Run(() =>
                {
                    lock (this)
                    {
                        string currentUrl = url;
                        currentUrl = currentUrl.Replace("{IMEI}", imei)
                            .Replace("{STATE}", engineState == EngineState.Started ? "1" : "0")
                            .Replace("{UTC}", utcDatetime.ToString(datetimeFormat));

                        HttpWebRequest webRequest = (HttpWebRequest)WebRequest.Create(currentUrl);
                        webRequest.Timeout = webRequestTimeout;
                        webRequest.Method = "GET";
                        try
                        {
                            var webResponse = (HttpWebResponse)webRequest.GetResponse();
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
    }
}
