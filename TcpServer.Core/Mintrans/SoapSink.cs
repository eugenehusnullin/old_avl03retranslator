using System;
using System.IO;
using System.Net;
using System.Threading.Tasks;

namespace TcpServer.Core.Mintrans
{
    public class SoapSink
    {
        private MintransSettings settings;

        public SoapSink(MintransSettings settings)
        {
            this.settings = settings;
        }

        public async Task PostSoapMessage(byte[] message)
        {
            await Task.Run(() =>
            {
                HttpWebRequest request = WebRequest.Create(this.settings.Url) as HttpWebRequest;
                try
                {
                    request.Method = "POST";
                    request.KeepAlive = true;
                    request.ContentType = "application/soap+xml";
                    request.ContentLength = message.Length;
                    request.Credentials = new NetworkCredential(this.settings.UserName, this.settings.Password);
                    using (Stream stream = request.GetRequestStream())
                    {
                        stream.Write(message, 0, message.Length);
                        HttpWebResponse response = request.GetResponse() as HttpWebResponse;
                        if (response.StatusCode == HttpStatusCode.OK)
                        {
                            using (Stream responseStream = response.GetResponseStream()) { }
                        }
                        else
                        {
                            throw new ApplicationException("PostSoapMessage: " + response.StatusDescription);
                        }
                    }

                    request.Abort();
                }
                catch
                {
                    request.Abort();
                    throw;
                }
            });
        }

        private void AssertTask(Task task)
        {
            if (task.IsFaulted)
            {
                throw new ApplicationException("SoapSink", task.Exception);
            }
        }
    }
}