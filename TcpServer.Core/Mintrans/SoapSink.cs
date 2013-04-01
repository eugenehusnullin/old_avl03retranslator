using System.IO;
using System.Net;
using System.Threading.Tasks;

namespace TcpServer.Core.Mintrans
{
    public class SoapSink
    {
        private SoapSinkSettings settings;

        public SoapSink(SoapSinkSettings settings)
        {
            this.settings = settings;
        }

        public async Task PostSoapMessage(byte[] message)
        {
            WebRequest request = WebRequest.Create(this.settings.Url);
            request.Method = "POST";
            request.ContentType = "application/soap+xml";
            request.ContentLength = message.Length;
            request.Credentials = new NetworkCredential(this.settings.UserName, this.settings.Password);
            using (Stream stream = request.GetRequestStream())
            {
                await stream.WriteAsync(message, 0, message.Length);
                WebResponse response = await request.GetResponseAsync();
                if ((response as HttpWebResponse).StatusCode == HttpStatusCode.OK)
                {
                    using (Stream responseStream = response.GetResponseStream()) { }
                }
            }
        }
    }
}