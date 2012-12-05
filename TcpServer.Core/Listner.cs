using System;
using System.IO;
using System.Net;
using System.Net.Sockets;
using System.Text;
using System.Threading;

namespace TcpServer.Core
{
    public class Listner
    {
        public string SrcHost { get; set; }
        public int SrcPort { get; set; }
        public Logger Logger { get; set; }

        public Listner(string srcIpAddress, int srcPort)
        {
            SrcHost = srcIpAddress;
            SrcPort = srcPort;
            Logger = new Logger(null, "listnerLog");
        }

        public void Start()
        {
            var localAddr = IPAddress.Parse(SrcHost);

            var tcpListener = new TcpListener(localAddr, SrcPort);
            try
            {
                // Start listening for client requests.
                tcpListener.Start();

                // Enter the listening loop.
                while (true)
                {
                    Console.WriteLine("Waiting for a connection... ");
                    var tcpClient = tcpListener.AcceptTcpClient();
                    ThreadPool.QueueUserWorkItem(DoProcess, tcpClient);
                    Console.WriteLine("New client connected!");
                }
            }
            catch (SocketException e)
            {
                Console.WriteLine("SocketException: {0}", e);
            }
            finally
            {
                tcpListener.Stop();
            }

            Console.WriteLine("\nHit enter to continue...");
            Console.Read();
        }

        private void DoProcess(Object clientObject)
        {
            var client = (TcpClient)clientObject;
            var stream = client.GetStream();

            try
            {

                // Buffer for reading data
                var bytes = new Byte[256];

                int i;
                // Loop to receive all the data sent by the client.
                while ((i = stream.Read(bytes, 0, bytes.Length)) != 0)
                {
                    // Translate data bytes to a ASCII string.
                    var data = Encoding.ASCII.GetString(bytes, 0, i);
                    Console.WriteLine("Received: {0}", data);

                    File.AppendAllText("recive.log", data, Encoding.Default);

                    //// Process the data sent by the client.
                    //data = data.ToUpper();

                    //var msg = System.Text.Encoding.ASCII.GetBytes(data);

                    //// Send back a response.
                    //stream.Write(msg, 0, msg.Length);
                    //Console.WriteLine("Sent: {0}", data);
                }
                //var data = "";
                //data += "7E7E7E7E00000000";
                //data += "0000000000000000";
                //data += "0000000000000000";
                //data += "0000000000000000";
                //data += "000000000D000004";
                //data += "02230000";
                //var msg = ByteHelper.GetBytesFromByteString(data);

                //// Send back a response.
                //stream.Write(msg, 0, msg.Length);
                //Console.WriteLine("Sent: {0}", data);
                //Logger.MessageWriteLine(string.Format("Sent: {0}", data));

                //var buffer = new Byte[256];
                //var k = stream.Read(buffer, 0, buffer.Length);
                //var responseData = ByteHelper.GetStringFromBytes(buffer,0,k);
                //Console.WriteLine("Received: {0}", responseData);
                //Logger.MessageWriteLine(string.Format("Received: {0}", responseData));
            }
            catch (Exception e)
            {
                Console.WriteLine("Exception: {0}", e);
            }
            finally
            {
                stream.Close();
                client.Close();
            }
        }
    }

}
