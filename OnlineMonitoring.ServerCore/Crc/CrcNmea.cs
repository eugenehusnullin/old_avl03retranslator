using System.Text;

namespace OnlineMonitoring.ServerCore.Crc
{
    public class CrcNmea
    {
        public static ushort ComputeChecksum(byte[] bytes)
        {
            ushort crc = 0x0;

            foreach (var t in bytes)
            {
                crc = (ushort)(crc ^ t);
            }

            return crc;
        }

        public static ushort ComputeChecksumASCII(string text)
        {
            var bytes = Encoding.ASCII.GetBytes(text);
            return ComputeChecksum(bytes);
        } 
    }
}