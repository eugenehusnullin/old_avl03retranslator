using System;
using System.Globalization;

namespace TcpServer.Core.Mintrans
{
    public class MintransMapper
    {
        public virtual string MapTime(BasePacket packet)
        {
            return packet.ValidNavigDateTime.ToString("yyyy-MM-ddTHH:mm:ssZ", CultureInfo.InvariantCulture.DateTimeFormat);
        }

        public virtual string MapLat(BasePacket packet)
        {
            string text = (packet.Latitude / 100).ToString(CultureInfo.InvariantCulture.NumberFormat);
            return text.Substring(0, Math.Min(15, text.Length));
        }

        public virtual string MapLon(BasePacket packet)
        {
            string text = (packet.Longitude / 100).ToString(CultureInfo.InvariantCulture.NumberFormat);
            return text.Substring(0, Math.Min(15, text.Length));
        }

        public virtual string MapSpeed(BasePacket packet)
        {
            return packet.Speed.ToString("0.0", CultureInfo.InvariantCulture.NumberFormat);
        }

        public virtual string MapDir(BasePacket packet)
        {
            return packet.Direction.ToString("0", CultureInfo.InvariantCulture.NumberFormat);
        }

        public virtual string MapValid(BasePacket packet)
        {
            return ('A' == packet.State) ? "1": "0";
        }
    }
}