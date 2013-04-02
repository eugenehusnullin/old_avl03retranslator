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
        private MintransSink target;

        [SetUp]
        public void Setup()
        {
            Mock<ILog> log = new Mock<ILog>();
            SoapSink soapSink = new SoapSink(new SoapSinkSettings());
            MessageBuilder builder = new MessageBuilder(new MintransMapper());
            this.target = new MintransSink(log.Object, soapSink, builder);
        }

        [Test]
        public void Test()
        {
            BasePacket packet = BasePacket.GetFromGlonass(PACKET);
            Task task = this.target.SendLocationAndState(packet);
            task.Wait();
            Assert.IsNull(task.Exception);
            Assert.IsFalse(task.IsFaulted);
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
            System.Console.WriteLine("ERROR CHECKING");
            foreach (Task t in tasks)
            {
                if (t.IsFaulted)
                {
                    System.Console.WriteLine(t.Exception);
                }
            }

            tasks = new Task[50];
            for (int i = 0; i < 50; i++)
            {
                tasks[i] = Task.Run(() => { this.target.SendLocationAndState(packet); });
            }

            Task.WaitAll(tasks);
            System.Console.WriteLine("ERROR CHECKING");
            foreach (Task t in tasks)
            {
                if (t.IsFaulted)
                {
                    System.Console.WriteLine(t.Exception);
                }
            }
        }
    }
}