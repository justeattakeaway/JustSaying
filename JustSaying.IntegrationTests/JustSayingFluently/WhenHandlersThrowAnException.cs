using System;
using JustBehave;
using JustSaying.TestingFramework;
using NSubstitute;

namespace JustSaying.IntegrationTests.JustSayingFluently
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
        public void ThenExceptionIsRecordedInMonitoring()
        {
            _handler.WaitUntilCompletion(10.Seconds()).ShouldBeTrue();
            
           Patiently.VerifyExpectation(()=> Monitoring.Received().HandleException(Arg.Any<string>()));
        }

       
    }
}