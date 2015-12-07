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

        protected override void When()
        {
            ServiceBus.Publish(new GenericMessage());
        }

        [Test]
        public async Task ThenExceptionIsRecordedInMonitoring()
        {
            _handler.WaitUntilCompletion(15.Seconds()).ShouldBe(true);

            await Patiently.VerifyExpectationAsync(
                () => Monitoring.Received().HandleException(Arg.Any<string>()));
        }
    }
}