using log4net;
using System;
using System.Threading.Tasks;

namespace TcpServer.Core.Mintrans
{
    public class MintransSink
    {
        private ILog log;
        private MintransSettings settings;
        private ObjectPool<SoapSink> soapSinkPool;
        private MessageBuilder builder;
        private ImeiList imeiList;

        public static MintransSink GetInstance(ILog log)
        {
            MintransSettings settings = new MintransSettings();
            return new MintransSink(log, settings, new MessageBuilder(new MintransMapper()), new ImeiList(settings));
        }

        public MintransSink(
            ILog log,
            MintransSettings settings,
            MessageBuilder builder,
            ImeiList imeiList)
        {
            this.log = log;
            this.settings = settings;
            this.soapSinkPool = new ObjectPool<SoapSink>(20, () => new SoapSink(this.settings));
            this.builder = builder;
            this.imeiList = imeiList;
        }

        public async void SendLocationAndState(BasePacket packet)
        {
            SoapSink sink = this.soapSinkPool.GetFromPool();
            try
            {
                if (this.settings.Enabled &&
                    this.imeiList.Contains(packet.IMEI))
                {
                    byte[] messageBytes = this.builder.CreateLocationAndStateMessage(packet);
                    await sink.PostSoapMessage(messageBytes);
                }
            }
            catch (Exception ex)
            {
                this.log.Error("MintransSink.SendLocationAndState " + ex.ToString());
            }
            finally
            {
                this.soapSinkPool.ReturnToPool(sink);
            }
        }
    }
}