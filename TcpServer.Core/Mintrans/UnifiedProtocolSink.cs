using log4net;
using System;
using System.Threading.Tasks;

namespace TcpServer.Core.Mintrans
{
    public class UnifiedProtocolSink
    {
        private ILog log;
        private IUnifiedProtocolSettings settings;
        private ObjectPool<SoapSink> soapSinkPool;
        private MessageBuilder builder;
        private ImeiList imeiList;

        public static UnifiedProtocolSink GetInstance(IUnifiedProtocolSettings settings)
        {
            return new UnifiedProtocolSink(settings, new MessageBuilder(new MintransMapper()), new ImeiList(settings));
        }

        public UnifiedProtocolSink(
            IUnifiedProtocolSettings settings,
            MessageBuilder builder,
            ImeiList imeiList)
        {            
            this.settings = settings;
            this.soapSinkPool = new ObjectPool<SoapSink>(20, () => new SoapSink(this.settings));
            this.builder = builder;
            this.imeiList = imeiList;

            this.log = LogManager.GetLogger(settings.LoggerName);
        }

        public async void SendLocationAndState(BasePacket packet)
        {
            if (this.settings.Enabled &&
                    this.imeiList.Contains(packet.IMEI))
            {
                SoapSink sink = this.soapSinkPool.GetFromPool();
                try
                {
                    byte[] messageBytes = this.builder.CreateLocationAndStateMessage(packet);
                    await sink.PostSoapMessage(messageBytes);
                    this.log.InfoFormat("Retranslated to [{0}] IMEI= {1}, geo= {2}, {3}", this.settings.Url, packet.IMEI, packet.Latitude, packet.Longitude);

                }
                catch (Exception ex)
                {
                    this.log.Error("UnifiedProtocolSink.SendLocationAndState: " + this.settings.Url + ": " + ex.ToString());
                }
                finally
                {
                    this.soapSinkPool.ReturnToPool(sink);
                }
            }
        }
    }
}