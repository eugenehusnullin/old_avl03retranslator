using log4net;
using System.Threading.Tasks;

namespace TcpServer.Core.Mintrans
{
    public class MintransSink
    {
        private ILog log;
        private SoapSink sink;
        private MessageBuilder builder;

        public static MintransSink GetInstance(ILog log)
        {
            return new MintransSink(log, new SoapSink(new SoapSinkSettings()), new MessageBuilder(new MintransMapper()));
        }

        public MintransSink(
            ILog log,
            SoapSink sink,
            MessageBuilder builder)
        {
            this.log = log;
            this.sink = sink;
            this.builder = builder;
        }

        public async Task SendLocationAndState(BasePacket packet)
        {
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