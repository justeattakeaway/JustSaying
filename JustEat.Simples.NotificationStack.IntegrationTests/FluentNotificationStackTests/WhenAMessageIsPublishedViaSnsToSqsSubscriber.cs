using System;
using System.Threading;
using JustEat.Simples.NotificationStack.Stack;
using NSubstitute;
using NUnit.Framework;
using Tests.MessageStubs;

namespace NotificationStack.IntegrationTests.FluentNotificationStackTests
{
    public class WhenAMessageIsPublishedViaSnsToSqsSubscriber : GivenANotificationStack
    {
        private readonly Future<GenericMessage> _handler = new Future<GenericMessage>();

        protected override IFluentNotificationStack CreateSystemUnderTest()
        {
            base.CreateSystemUnderTest();
            ServiceBus.WithMessageHandler(_handler);
            ServiceBus.StartListening();
            return ServiceBus;
        }

        protected override void When()
        {
            ServiceBus.Publish(new GenericMessage());
        }

        [Test]
        public void ThenItGetsHandled()
        {
            _handler.WaitUntilCompletion(2.Seconds()).ShouldBeTrue();
        }
    }

    public class WhenHandlersThrowAnException : GivenANotificationStack
    {
        private readonly Future<GenericMessage> _handler = new Future<GenericMessage>(() => { throw new Exception(""); });
        protected override void Given()
        {
            RecordAnyExceptionsThrown();
            base.Given();
        }

        protected override IFluentNotificationStack CreateSystemUnderTest()
        {
            base.CreateSystemUnderTest();
            ServiceBus.WithMessageHandler(_handler);
            ServiceBus.StartListening();
            return ServiceBus;
        }

        protected override void When()
        {
            ServiceBus.Publish(new GenericMessage());
        }

        [Test]
        public void ThenExceptionIsRecordedInStatsD()
        {
            _handler.WaitUntilCompletion(10.Seconds()).ShouldBeTrue();
            Thread.Sleep(1000);
            Monitoring.Received().HandleException(Arg.Any<string>());
        }
    }
}
