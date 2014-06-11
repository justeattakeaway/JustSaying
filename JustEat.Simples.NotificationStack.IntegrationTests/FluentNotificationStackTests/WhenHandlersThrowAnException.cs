using System;
using System.Threading;
using JustEat.Testing;
using JustSaying.AwsTools;
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
            _handler.WaitUntilCompletion(10.Seconds()).ShouldBeTrue();
            Thread.Sleep(2000);
            Monitoring.Received().HandleException(Arg.Any<string>());
        }
    }
}