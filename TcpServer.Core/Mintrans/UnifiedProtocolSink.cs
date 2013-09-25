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

            if (this.settings.Enabled) 
            {
                var id = this.imeiList.GetId(packet.IMEI);
                if (id != null)
                {
                    SoapSink sink = this.soapSinkPool.GetFromPool();
                    try
                    {
                        byte[] messageBytes = this.builder.CreateLocationAndStateMessage(packet, id);
                        await sink.PostSoapMessageAsync(messageBytes);
                        this.log.InfoFormat(
                            packet.isSOS() ? "ALARM = SOS, IMEI={0}, geo={1}, {2}, speed={3}, direction={4}, altitude={5}, state={6}, id={7}" 
                            : "IMEI={0}, geo={1}, {2}, speed={3}, direction={4}, altitude={5}, state={6}, id={7}",
                            packet.IMEI,
                            packet.Latitude,
                            packet.Longitude,
                            packet.Speed,
                            packet.Direction,
                            packet.Altitude,
                            packet.State,
                            id);
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
}