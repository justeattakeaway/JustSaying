using System;
using System.Threading.Tasks;
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
            _handler = new Future<GenericMessage>(() => { throw new Exception("Test Exception"); });
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
            _handler.MessageCount.ShouldBeGreaterThan(0);

            Monitoring.Received().HandleException(Arg.Any<string>());
        }
    }
}