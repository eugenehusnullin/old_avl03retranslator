using System;
using System.IO;
using System.Net;
using System.Threading.Tasks;

namespace TcpServer.Core.Mintrans
{
    public class SoapSink
    {
        private SoapSinkSettings settings;
        private ObjectPool<WebRequest> pool;

        public SoapSink(SoapSinkSettings settings)
        {
            this.settings = settings;
            this.pool = new ObjectPool<WebRequest>(40, () => WebRequest.Create(this.settings.Url));
        }

        public async Task PostSoapMessage(byte[] message)
        {
            await Task.Run(() =>
            {
                WebRequest request = this.pool.GetFromPool();
                try
                {
                    request.Method = "POST";
                    request.ContentType = "application/soap+xml";
                    request.ContentLength = message.Length;
                    request.Credentials = new NetworkCredential(this.settings.UserName, this.settings.Password);
                    using (Stream stream = request.GetRequestStream())
                    {
                        stream.Write(message, 0, message.Length);
                        WebResponse response = request.GetResponse();
                        if ((response as HttpWebResponse).StatusCode == HttpStatusCode.OK)
                        {
                            using (Stream responseStream = response.GetResponseStream()) { }
                        }
                    }

                    request.Abort();
                    this.pool.ReturnToPool(request);
                }
                catch
                {
                    request.Abort();
                    this.pool.DestroyOne();
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