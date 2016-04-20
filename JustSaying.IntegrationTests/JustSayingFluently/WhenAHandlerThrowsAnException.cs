using System;
using System.Threading.Tasks;
using Amazon;
using JustBehave;
using JustSaying.Messaging.MessageHandling;
using JustSaying.Messaging.Monitoring;
using JustSaying.TestingFramework;
using NSubstitute;
using NUnit.Framework;

namespace JustSaying.IntegrationTests.JustSayingFluently
{
    [TestFixture]
    public class WhenAHandlerThrowsAnException
    {
        private readonly IHandlerAsync<GenericMessage> _handler = Substitute.For<IHandlerAsync<GenericMessage>>();
        private Action<Exception, Amazon.SQS.Model.Message> _globalErrorHandler;
        private bool _handledException;
        private IMessageMonitor _monitoring;

        [OneTimeSetUp]
        public async Task Setup()
        {
            // Setup
            var doneSignal = new TaskCompletionSource<object>();
            _globalErrorHandler = (ex, m) => { _handledException = true; };
            _monitoring = Substitute.For<IMessageMonitor>();

            // Given
            _handler.Handle(Arg.Any<GenericMessage>())
                .Returns(true)
                .AndDoes(_ =>
                {
                    Tasks.DelaySendDone(doneSignal);
                    throw new TestException("My Ex");
                });

            var bus = CreateMeABus.InRegion(RegionEndpoint.EUWest1.SystemName)
                .WithMonitoring(_monitoring)
                .ConfigurePublisherWith(c =>
                    {
                        c.PublishFailureBackoffMilliseconds = 1;
                        c.PublishFailureReAttempts = 3;
                    })
                .WithSnsMessagePublisher<GenericMessage>()
                .WithSqsTopicSubscriber()
                .IntoQueue("queuename")
                .ConfigureSubscriptionWith(cfg =>
                    {
                        cfg.MessageRetentionSeconds = 60;
                        cfg.InstancePosition = 1;
                        cfg.OnError = _globalErrorHandler;
                    })
                .WithMessageHandler(_handler);

            // When 
            bus.StartListening();

            bus.Publish(new GenericMessage());

            // Teardown
            await doneSignal.Task;
            bus.StopListening();
        }

        [Then]
        public void MessagePopsOutAtTheOtherEnd()
        {
             _handler.Received().Handle(Arg.Any<GenericMessage>());
        }

        [Then]
        public void CustomExceptionHandlingIsCalled()
        {
            Assert.That(_handledException, Is.True);
        }
    }
}
