using System;
using System.Diagnostics;
using System.Net.Sockets;
using System.Threading;
using OnlineMonitoring.ServerCore.DataBase;
using OnlineMonitoring.ServerCore.Helpers;
using OnlineMonitoring.ServerCore.Packets;

namespace OnlineMonitoring.ServerCore.Listners
{
    public class WialonListner : BaseListner
    {
        public WialonListner(string srcIpAddress, int srcPort, EventLog eventLog, Options options)
            : base(srcIpAddress, srcPort, eventLog, options) { }

        protected override void StreamProcessing(NetworkStream stream, DataBaseManager dataBaseManager)
        {
            while (State != ServiceState.Stoping)
            {
                if (!stream.CanRead) throw new Exception("Невозможно получить данные от ретранслятора");

                // Buffer for reading data lenght
                var bytes = new Byte[4];
                int i;
                lock (stream)
                {
                    i = stream.Read(bytes, 0, bytes.Length);
                }

                if (i > 0)
                {

                    var bufferSize = BitConverter.ToInt32(bytes, 0);

                    var buffer = new Byte[bufferSize];
                    lock (stream)
                    {
                        stream.Read(buffer, 0, buffer.Length);
                    }

#if DEBUG
                    var packet = ByteHelper.GetStringFromBytes(buffer);
                    Logger.PacketWriteLine(packet);
#endif

                    var basePacket = BasePacket.GetFromWialon(buffer);
                    dataBaseManager.SaveBasePacket(basePacket);

                    var resultBuffer = new[] { (byte)0x11 };
                    lock (stream)
                    {
                        stream.Write(resultBuffer, 0, resultBuffer.Length);
                    }
                }

                Thread.Sleep(50);
            }
        }
    }
}