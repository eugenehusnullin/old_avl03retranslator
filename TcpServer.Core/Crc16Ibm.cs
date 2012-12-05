using System;
using System.Text;

namespace TcpServer.Core
{
    public class Crc16Ibm
    {
        const ushort Polynomial = 0xA001;
        readonly ushort[] _table = new ushort[256];

        public ushort ComputeChecksum(byte[] bytes)
        {
            ushort crc = 0xffff;
            foreach (var t in bytes)
            {
                var index = (byte)(crc ^ t);
                crc = (ushort)((crc >> 8) ^ _table[index]);
            }
            return crc;
        }

        public byte[] ComputeChecksumBytes(byte[] bytes)
        {
            var crc = ComputeChecksum(bytes);
            return BitConverter.GetBytes(crc);
        }

        public ushort ComputeChecksumASCII(string text)
        {
            var bytes = Encoding.ASCII.GetBytes(text);
            return ComputeChecksum(bytes);
        }

        public Crc16Ibm()
        {
            for (ushort i = 0; i < _table.Length; ++i)
            {
                ushort value = 0;
                var temp = i;
                for (byte j = 0; j < 8; ++j)
                {
                    if (((value ^ temp) & 0x0001) != 0)
                    {
                        value = (ushort)((value >> 1) ^ Polynomial);
                    }
                    else
                    {
                        value >>= 1;
                    }
                    temp >>= 1;
                }
                _table[i] = value;
            }
        }
    }
}