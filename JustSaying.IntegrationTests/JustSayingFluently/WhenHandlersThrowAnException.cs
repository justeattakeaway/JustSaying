using System;
using System.Threading.Tasks;
using JustSaying.IntegrationTests.TestHandlers;
using JustSaying.TestingFramework;
using NSubstitute;
using Shouldly;
using Xunit;

namespace JustSaying.IntegrationTests.JustSayingFluently
{
    [Collection(GlobalSetup.CollectionName)]
    public class WhenHandlersThrowAnException : GivenANotificationStack
    {
        private Future<SimpleMessage> _handler;

        protected override async Task Given()
        {
            RecordAnyExceptionsThrown();

            await base.Given();
            _handler = new Future<SimpleMessage>(() => throw new TestException("Test Exception from WhenHandlersThrowAnException"));
            RegisterSnsHandler(_handler);
        }

        protected override async Task When()
        {
            await ServiceBus.PublishAsync(new SimpleMessage());
            await _handler.DoneSignal;
        }

        [AwsFact]
        public void ThenExceptionIsRecordedInMonitoring()
        {
            _handler.ReceivedMessageCount.ShouldBeGreaterThan(0);

            Monitoring.Received().HandleException(Arg.Any<Type>());
        }
    }
}
