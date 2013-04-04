using System.Threading;
using System.Threading.Tasks;
using log4net;
using Moq;
using NUnit.Framework;
using TcpServer.Core;
using TcpServer.Core.Mintrans;
using log4net.Config;

namespace TcpServer.IntegrationTests.Mintrans.MintransSinkTest
{
    [TestFixture]
    public class SendLocationAndStateTest
    {
        private string PACKET = "$$9F359772035610485|AAUA0855.99485N038.37350E000000|01.6|01.0|01.3|20130324200047|20130324200047|000100000000|14122425|08580121|13DAC2AB|0000|0.0000|0167||580F";
        private UnifiedProtocolSink target;

        [SetUp]
        public void Setup()
        {
            XmlConfigurator.Configure();
            ILog log = LogManager.GetLogger("Main");
            IUnifiedProtocolSettings settings = new MintransMoscowRegionSettings();
            SoapSink soapSink = new SoapSink(settings);
            MessageBuilder builder = new MessageBuilder(new MintransMapper());
            ImeiList imeiExclusionList = new ImeiList(settings);
            this.target = new UnifiedProtocolSink(settings, builder, imeiExclusionList);
        }

        [Test]
        public void Test()
        {
            BasePacket packet = BasePacket.GetFromGlonass(PACKET);
            this.target.SendLocationAndState(packet);
        }

        [Test]
        public void Stress()
        {
            BasePacket packet = BasePacket.GetFromGlonass(PACKET);
            Task[] tasks = new Task[50];
            for (int i = 0; i < 50; i++)
            {
                tasks[i] = Task.Run(() => { this.target.SendLocationAndState(packet); });
            }

            Task.WaitAll(tasks);
            
        }
    }
}