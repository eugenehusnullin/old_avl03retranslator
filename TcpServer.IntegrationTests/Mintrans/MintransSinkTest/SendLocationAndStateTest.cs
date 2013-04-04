using System.Threading;
using System.Threading.Tasks;
using log4net;
using Moq;
using NUnit.Framework;
using TcpServer.Core;
using TcpServer.Core.Mintrans;

namespace TcpServer.IntegrationTests.Mintrans.MintransSinkTest
{
    [TestFixture]
    public class SendLocationAndStateTest
    {
        private string PACKET = "$$9F359772038626256|AAUA0855.99485N038.37350E000000|01.6|01.0|01.3|20130324200047|20130324200047|000100000000|14122425|08580121|13DAC2AB|0000|0.0000|0167||580F";
        private UnifiedProtocolSink target;

        [SetUp]
        public void Setup()
        {
            MintransMoscowRegionSettings settings = new MintransMoscowRegionSettings();
            Mock<ILog> log = new Mock<ILog>();
            SoapSink soapSink = new SoapSink(settings);
            MessageBuilder builder = new MessageBuilder(new MintransMapper());
            ImeiList imeiExclusionList = new ImeiList(settings);
            this.target = new UnifiedProtocolSink(log.Object, settings, builder, imeiExclusionList);
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