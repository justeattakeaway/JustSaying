using System.Threading.Tasks;
using JustSaying.IntegrationTests.TestHandlers;
using JustSaying.TestingFramework;
using NSubstitute;
using NUnit.Framework;
using Shouldly;

namespace JustSaying.IntegrationTests.JustSayingFluently
{
    public class WhenHandlersThrowAnException : GivenANotificationStack
    {
        private Future<GenericMessage> _handler;

        protected override void Given()
        {
            RecordAnyExceptionsThrown();

            base.Given();
            _handler = new Future<GenericMessage>(() =>
            {
                throw new TestException("Test Exception from WhenHandlersThrowAnException");
            });
            RegisterSnsHandler(_handler);
        }

        protected override async Task When()
        {
            ServiceBus.Publish(new GenericMessage());
            await _handler.DoneSignal;
        }

        [Test]
        public void ThenExceptionIsRecordedInMonitoring()
        {
            _handler.ReceivedMessageCount.ShouldBeGreaterThan(0);

            Monitoring.Received().HandleException(Arg.Any<string>());
        }
    }
}