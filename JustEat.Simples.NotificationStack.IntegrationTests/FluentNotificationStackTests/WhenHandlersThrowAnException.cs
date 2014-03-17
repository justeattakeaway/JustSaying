using System;
using System.Linq;
using System.Threading;
using Amazon;
using Amazon.SQS.Model;
using JustEat.Simples.NotificationStack.AwsTools;
using JustEat.Simples.NotificationStack.Stack;
using JustEat.Testing;
using NSubstitute;
using NUnit.Framework;
using Tests.MessageStubs;

namespace NotificationStack.IntegrationTests.FluentNotificationStackTests
{
    public class WhenHandlersThrowAnException : GivenANotificationStack
    {
        private readonly Future<GenericMessage> _handler = new Future<GenericMessage>(() => { throw new Exception(""); });
        private SqsQueueByName _queue;
        private string _component;

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

        public override void PostAssertTeardown()
        {
            if(_queue!= null)
                 _queue.Delete();
        }
    }
}