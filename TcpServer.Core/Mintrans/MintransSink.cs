using log4net;
using System;
using System.Threading.Tasks;

namespace TcpServer.Core.Mintrans
{
    public class MintransSink
    {
        private ILog log;
        private MintransSettings settings;
        private SoapSink sink;
        private MessageBuilder builder;
        private ImeiList imeiList;

        public static MintransSink GetInstance(ILog log)
        {
            MintransSettings settings = new MintransSettings();
            return new MintransSink(log, settings, new SoapSink(settings), new MessageBuilder(new MintransMapper()), new ImeiList(settings));
        }

        public MintransSink(
            ILog log,
            MintransSettings settings,
            SoapSink sink,
            MessageBuilder builder,
            ImeiList imeiList)
        {
            this.log = log;
            this.settings = settings;
            this.sink = sink;
            this.builder = builder;
            this.imeiList = imeiList;
        }

        public async void SendLocationAndState(BasePacket packet)
        {
            try
            {
                if (!this.settings.Enabled || !this.imeiList.Contains(packet.IMEI))
                {
                    return;
                }

                byte[] messageBytes = this.builder.CreateLocationAndStateMessage(packet);
                await this.sink.PostSoapMessage(messageBytes);
            }
            catch(Exception ex)
            {
                this.log.Error("MintransSink.SendLocationAndState " + ex.ToString());
            }
        }
    }
}