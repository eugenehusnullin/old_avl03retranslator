using System;
using System.IO;
using System.Net.Sockets;
using System.Threading.Tasks;

namespace TcpServer.Core.Mintrans
{
    public class SoapSink
    {
        private const int ATTEMPTS_COUNT = 1;
        private const string INIT_MESSAGE = "POST {0} HTTP/1.1\r\n" +
                            "Content-Type: application/soap+xml\r\n" +
                            "Host: {1}:{2}\r\n" +
                            "Content-Length: 5\r\n" +
                            "Expect: 100-continue\r\n" +
                            "Connection: Keep-Alive\r\n" +
                            "\r\n" +
                            "HELLO";

        private const string POST_MESSAGE = "POST {0} HTTP/1.1\r\n" +
                            "Content-Type: application/soap+xml\r\n" +
                            "{1}" +
                            "Host: {2}:{3}\r\n" +
                            "Content-Length: {4}\r\n" +
                            "Expect: 100-continue\r\n" +
                            "Connection: Keep-Alive\r\n" +
                            "\r\n";

        private IUnifiedProtocolSettings settings;
        private TcpClient client;
        private StreamReader reader;

        public SoapSink(IUnifiedProtocolSettings settings)
        {
            this.settings = settings;
        }

        public bool Connected
        {
            get
            {
                return null != this.client && this.client.Connected;
            }
        }

        public bool IsSecure
        {
            get
            {
                return !string.IsNullOrEmpty(this.settings.UserName);
            }
        }

        public async Task PostSoapMessageAsync(byte[] message)
        {
            await Task.Run(() =>
            {
                lock (this)
                {
                    string responseText = null;
                    Exception ex = null;
                    bool success = this.SendMessageInAttempts(message, ref responseText, ref ex);
                    if (!success)
                    {
                        throw new ApplicationException(responseText, ex);
                    }
                }
            });
        }

        public void PostSoapMessage(byte[] message)
        {
            string responseText = null;
            Exception ex = null;
            bool success = this.SendMessageInAttempts(message, ref responseText, ref ex);
            if (!success)
            {
                throw new ApplicationException(responseText, ex);
            }
        }

        private bool SendMessageInAttempts(byte[] message, ref string responseText, ref Exception responseException)
        {
            bool success = false;
            int attempt = 1;
            char[] responseBuffer = new char[1] { '0' };
            while (!success)
            {
                if (attempt > ATTEMPTS_COUNT)
                {
                    responseText = MessageBuilder.ENCODING.GetString(MessageBuilder.ENCODING.GetBytes(responseBuffer));
                    break;
                }

                try
                {
                    if (!this.Connected)
                    {
                        this.Connect();
                    }

                    success = this.Send(message, out responseBuffer);
                }
                catch(Exception ex)
                {
                    success = false;
                    responseException = ex;
                    this.Close();
                }

                attempt++;
            }

            return success;
        }

        private void Close()
        {
            try
            {
                this.client.Close();
                this.client = null;
            }
            catch { }
        }

        private void Connect()
        {
            Uri uri = new Uri(this.settings.Url);
            this.client = new TcpClient();
            this.client.ReceiveTimeout = 10 * 1000;
            this.client.SendTimeout = 20 * 1000;
            this.client.Connect(uri.Host, uri.Port);
            NetworkStream stream = this.client.GetStream();
            StreamWriter writer = new StreamWriter(stream, MessageBuilder.ENCODING);
            this.reader = new StreamReader(stream, MessageBuilder.ENCODING);

            if (this.IsSecure)
            {
                int contentLength = 0;
                writer.Write(string.Format(INIT_MESSAGE, uri.AbsolutePath, uri.Host, uri.Port));
                writer.Flush();
                bool success = this.ReadHeaders(reader, out contentLength);
                char[] buffer;
                this.RollStream(reader, contentLength, out buffer);
            }
        }

        private bool Send(byte[] message, out char[] buffer)
        {
            Uri uri = new Uri(this.settings.Url);
            string token = string.Empty;
            if (this.IsSecure)
            {
                token = string.Concat(
                    "Authorization: Basic ",
                    Convert.ToBase64String(MessageBuilder.ENCODING.GetBytes(this.settings.UserName + ":" + this.settings.Password)),
                    "\r\n");
            }

            string headersText = string.Format(POST_MESSAGE, uri.AbsolutePath, token, uri.Host, uri.Port, message.Length);
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
                    if (null == temp)
                    {
                        throw new IOException("Cannot read from socket.");
                    }

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
    }
}