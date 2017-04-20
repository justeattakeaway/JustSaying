using JustSaying.TestingFramework;
using NUnit.Framework;

namespace JustSaying.UnitTests.Traceability
{
    [TestFixture]
    public class TraceablityTests
    {
        [Test]
        public void ParentMessageIdIsRecorded()
        {
            var message = new OrderAccepted();

            var downStreamMessage = new DownStreamMessage(message);            

            Assert.That(downStreamMessage.CorrelationId, Is.EqualTo(message.Id));
        }
    }
}
