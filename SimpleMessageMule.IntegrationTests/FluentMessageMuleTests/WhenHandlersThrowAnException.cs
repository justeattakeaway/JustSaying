using System;
using System.Threading;
using JustEat.Simples.NotificationStack.AwsTools;
using JustEat.Testing;
using NSubstitute;
using SimpleMessageMule.TestingFramework;
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
        public void ThenExceptionIsRecordedInMonitoring()
        {
            _handler.WaitUntilCompletion(10.Seconds()).ShouldBeTrue();
            
           Patiently.VerifyExpectation(()=> Monitoring.Received().HandleException(Arg.Any<string>()));
        }

        public override void PostAssertTeardown()
        {
            if(_queue!= null)
                 _queue.Delete();
        }
    }
}