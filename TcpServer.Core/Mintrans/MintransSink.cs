using log4net;
using System.Threading.Tasks;

namespace TcpServer.Core.Mintrans
{
    public class MintransSink
    {
        private ILog log;
        private MintransSettings settings;
        private SoapSink sink;
        private MessageBuilder builder;
        private ImeiExclusionList imeiExclusionList;

        public static MintransSink GetInstance(ILog log)
        {
            MintransSettings settings = new MintransSettings();
            return new MintransSink(log, settings, new SoapSink(settings), new MessageBuilder(new MintransMapper()), new ImeiExclusionList(settings));
        }

        public MintransSink(
            ILog log,
            MintransSettings settings,
            SoapSink sink,
            MessageBuilder builder,
            ImeiExclusionList imeiExclusionList)
        {
            this.log = log;
            this.settings = settings;
            this.sink = sink;
            this.builder = builder;
            this.imeiExclusionList = imeiExclusionList;
        }

        public async Task SendLocationAndState(BasePacket packet)
        {
            if (false == this.settings.Enabled ||
                this.imeiExclusionList.IsExclusion(packet.IMEI))
            {
                return;
            }

            byte[] messageBytes = this.builder.CreateLocationAndStateMessage(packet);
            Task task = this.sink.PostSoapMessage(messageBytes);
            await task;
            if (task.IsFaulted)
            {
                this.log.Error("MintransSink.SendLocationAndState " + task.Exception.ToString());
            }
        }
    }
}