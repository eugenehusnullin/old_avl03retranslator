using System;
using System.Net.Sockets;
using System.Text;
using System.Threading;

namespace TcpServer.Core
{
    public class Sender
    {
        public Sender(string server, int port)
        {
            Server = server;
            Port = port;
            Logger = new Logger(null, "senderLog");
        }

        private string Server { get; set; }
        private int Port { get; set; }
        public Logger Logger { get; set; }

        public void Send(string message)
        {
            try
            {
                var client = new TcpClient(Server, Port);
                var stream = client.GetStream();

                try
                {
                    SendPacket(message, stream);
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
            catch (ArgumentNullException e)
            {
                Console.WriteLine("ArgumentNullException: {0}", e);
            }
            catch (SocketException e)
            {
                Console.WriteLine("SocketException: {0}", e);
            }

            Console.WriteLine("\n Press Enter to continue...");
            Console.Read();
        }

        private  void SendPacket(string message, NetworkStream stream)
        {
             //Translate the passed message into ASCII and store it as a Byte array.
            var data = Encoding.ASCII.GetBytes(message);

             //Send the message to the connected TcpServer. 
            stream.Write(data, 0, data.Length);

            Console.WriteLine("Sent: {0}", message);

            //Receive the TcpServer.response.

            
            //var buffer = new Byte[256];
            //var k = stream.Read(buffer, 0, buffer.Length);
            //var responseData = ByteHelper.GetStringFromBytes(buffer, 0, k);
            //Console.WriteLine("Received: {0}", responseData);
            //Logger.MessageWriteLine(string.Format("Received: {0}", responseData));


            //var data = "7E7E7E7E00000000000000000000000000000000000000000000000000000000000000005000002035303538000000000000000000000000885D9E0AB3E2C02659F0332E0B0F12";
            //var msg = ByteHelper.GetBytesFromByteString(data);
            //stream.Write(msg, 0, msg.Length);
            //Console.WriteLine("Sent: {0}", data);
            //Logger.MessageWriteLine(string.Format("Sent: {0}", data));

            //k = stream.Read(buffer, 0, buffer.Length);
            //responseData = Encoding.ASCII.GetString(buffer, 0, k);
            //Console.WriteLine("Received: {0}", responseData);
        }
    }
}