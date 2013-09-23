using System;
using System.Text;

namespace TcpServer.Core.Mintrans
{
    public class MessageBuilder
    {
        public static readonly Encoding ENCODING = Encoding.GetEncoding("windows-1251");
        private readonly MintransMapper mapper;

        public MessageBuilder(MintransMapper mapper)
        {
            this.mapper = mapper;
        }

        public byte[] CreateLocationAndStateMessage(BasePacket packet, string id)
        {
            string text = string.Format(packet.isSOS() ? SoapTemplates.Alarm : SoapTemplates.LocationAndState,
                id,
                this.mapper.MapTime(packet),
                this.mapper.MapLon(packet),
                this.mapper.MapLat(packet),
                packet.Altitude,
                this.mapper.MapSpeed(packet),
                this.mapper.MapDir(packet),
                this.mapper.MapValid(packet));

            return ENCODING.GetBytes(text);
        }
    }
}