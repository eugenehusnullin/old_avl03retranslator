namespace TcpServer.Core
{
    public class WialonBlockData
    {
        public short Type { get; set; }
        public int Lenght { get; set; }
        public bool IsHidden { get; set; }
        public byte DataType { get; set; }
        public string Name { get; set; }
        public byte[] Value { get; set; }
    }

    public class WialonPosInfo
    {
        public double Longitude { get; set; }
        public double Latitude { get; set; }
        public double Altitude { get; set; }
        public short Speed { get; set; }
        public short Cource { get; set; }
        public byte Satelits { get; set; }
    }
}