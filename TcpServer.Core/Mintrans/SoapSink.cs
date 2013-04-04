using System;
using System.IO;
using System.Net.Sockets;
using System.Threading.Tasks;

namespace TcpServer.Core.Mintrans
{
    public class SoapSink
    {
        private IUnifiedProtocolSettings settings;
        private bool initialized = false;
        private TcpClient client;
        private StreamReader reader;

        public SoapSink(IUnifiedProtocolSettings settings)
        {
            this.settings = settings;
        }

        public async Task PostSoapMessage(byte[] message)
        {
            await Task.Run(() =>
            {
                lock (this)
                {
                    if (false == this.initialized)
                    {
                        this.Initialize();
                    }

                    char[] buffer;
                    bool success = this.Send(message, out buffer);
                    if (false == success)
                    {
                        this.Initialize();
                        success = this.Send(message, out buffer);
                        if (false == success)
                        {
                            throw new ApplicationException(MessageBuilder.ENCODING.GetString(MessageBuilder.ENCODING.GetBytes(buffer)));
                        }
                    }
                }
            });
        }

        public void Reset()
        {
            try
            {
                this.client.Close();
                this.initialized = false;
            }
            catch { }
        }

        private void Initialize()
        {
            Uri uri = new Uri(this.settings.Url);
            this.client = new TcpClient();
            this.client.Connect(uri.Host, uri.Port);
            int contentLength = 0;
            NetworkStream stream = this.client.GetStream();
            StreamWriter writer = new StreamWriter(stream, MessageBuilder.ENCODING);
            writer.Write(INIT);
            writer.Flush();

            this.reader = new StreamReader(stream, MessageBuilder.ENCODING);
            bool success = this.ReadHeaders(reader, out contentLength);
            char[] buffer;
            this.RollStream(reader, contentLength, out buffer);
            this.initialized = true;
        }

        private bool Send(byte[] message, out char[] buffer)
        {
            Uri uri = new Uri(this.settings.Url);
            string token = Convert.ToBase64String(MessageBuilder.ENCODING.GetBytes(this.settings.UserName + ":" + this.settings.Password));
            string headersText = string.Format(this.POST, token, uri.Host, uri.Port, message.Length);
            byte[] headers = MessageBuilder.ENCODING.GetBytes(headersText);
            this.client.GetStream().Write(headers, 0, headers.Length);
            this.client.GetStream().Write(message, 0, message.Length);
            int contentLength = 0;
            bool success = this.ReadHeaders(this.reader, out contentLength);
            this.RollStream(this.reader, contentLength, out buffer);
            return success;
        }

        private bool ReadHeaders(StreamReader reader, out int contentLength)
        {
            string temp = null;
            bool ok = false;
            contentLength = 0;
            bool continueRead = true;
            while (continueRead)
            {
                continueRead = false;
                do
                {
                    temp = reader.ReadLine();
                    if (temp.StartsWith("HTTP"))
                    {
                        if (temp.EndsWith("Continue"))
                        {
                            continueRead = true;
                        }

                        ok = temp.EndsWith("OK");
                    }

                    if (temp.StartsWith("Content-Length: "))
                    {
                        contentLength = int.Parse(temp.Substring(16));
                    }
                }
                while (false == string.IsNullOrEmpty(temp));
            }

            return ok;
        }

        private void RollStream(StreamReader reader, int contentLength, out char[] buffer)
        {
            buffer = null;
            if (contentLength != 0)
            {
                buffer = new char[contentLength];
                reader.Read(buffer, 0, contentLength);
            }
        }

        private string INIT = "POST /gate2 HTTP/1.1\r\n" +
                            "Content-Type: application/soap+xml\r\n" +
                            "Host: 89.175.171.150:6400\r\n" +
                            "Content-Length: 5\r\n" +
                            "Expect: 100-continue\r\n" +
                            "Connection: Keep-Alive\r\n" +
                            "\r\n" +
                            "HELLO";

        private string POST = "POST /gate2 HTTP/1.1\r\n" +
                            "Content-Type: application/soap+xml\r\n" +
                            "Authorization: Basic {0}\r\n" +
                            "Host: {1}:{2}\r\n" +
                            "Content-Length: {3}\r\n" +
                            "Expect: 100-continue\r\n" +
                            "\r\n";
    }
}