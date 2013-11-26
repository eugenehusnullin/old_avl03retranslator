using log4net;
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
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

        private ConcurrentQueue<byte[]> messages;
        private int workersCount = 5;
        private List<Worker> workersList;

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
            this.messages = new ConcurrentQueue<byte[]>();
            workersList = new List<Worker>();
        }

        public void start()
        {
            for (int i = 0; i < workersCount; i++)
            {
                Worker worker = new Worker(settings, messages, log);
                workersList.Add(worker);
                worker.start();
            }
        }

        public void stop()
        {
            foreach (Worker worker in workersList)
            {
                worker.stop();
            }
        }

        public async void SendLocationAndStateAsync(BasePacket packet)
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

        public void SendLocationAndState(BasePacket packet)
        {
            if (settings.Enabled) 
            {
                var id = imeiList.GetId(packet.IMEI);
                if (id != null)
                {
                    try
                    {
                        byte[] messageBytes = builder.CreateLocationAndStateMessage(packet, id);
                        messages.Enqueue(messageBytes);
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
                }
            }
        }
    }
}