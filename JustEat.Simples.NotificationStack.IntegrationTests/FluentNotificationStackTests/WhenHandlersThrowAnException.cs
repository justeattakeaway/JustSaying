using System;
using JustEat.Testing;
using JustSaying.Tests.MessageStubs;
using NSubstitute;

namespace NotificationStack.IntegrationTests.FluentNotificationStackTests
{
    public class WhenHandlersThrowAnException : GivenANotificationStack
    {
        private readonly Future<GenericMessage> _handler = new Future<GenericMessage>(() => { throw new Exception(""); });

        protected override void Given()
        {
            RecordAnyExceptionsThrown();
            base.Given();
            RegisterHandler(_handler);
        }

        protected override void When()
        {
            ServiceBus.Publish(new GenericMessage());
        }

        [Then]
        public void ThenExceptionIsRecordedInStatsD()
        {
            _handler.WaitUntilCompletion(2.Seconds()).ShouldBeFalse();
            Monitoring.Received().HandleException(Arg.Any<string>());
        }
    }
}