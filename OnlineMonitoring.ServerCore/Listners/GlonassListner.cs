using System;
using System.Diagnostics;
using System.Net.Sockets;
using System.Text;
using OnlineMonitoring.ServerCore.DataBase;
using OnlineMonitoring.ServerCore.Packets;

namespace OnlineMonitoring.ServerCore.Listners
{
    public class GlonassBaseListner : BaseListner
    {
        public GlonassBaseListner(string srcIpAddress, int srcPort, EventLog eventLog, Options options)
            : base(srcIpAddress, srcPort, eventLog, options) { }

        protected override void StreamProcessing(NetworkStream stream, DataBaseManager dataBaseManager)
        {
            // Buffer for reading data
            var bytes = new Byte[256];
            int i;
            // Loop to receive all the data sent by the client.
            while ((i = stream.Read(bytes, 0, bytes.Length)) != 0)
            {
                // Translate data bytes to a ASCII string.
                var data = Encoding.ASCII.GetString(bytes, 0, i);
                Debug.WriteLine(string.Format("Received: {0}", data));

                var basePacket = BasePacket.GetFromGlonass(data);

                Debug.WriteLine("Save to data base");

                dataBaseManager.SaveBasePacket(basePacket);
            }
        }
    }
}