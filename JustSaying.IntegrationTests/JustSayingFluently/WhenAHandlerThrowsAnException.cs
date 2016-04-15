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
        private readonly IHandler<GenericMessage> _handler = Substitute.For<IHandler<GenericMessage>>();
        private Action<Exception, Amazon.SQS.Model.Message> _globalErrorHandler;
        private bool _handledException;
        private IMessageMonitor _monitoring;

        private TaskCompletionSource<object> _doneSignal;

        [OneTimeSetUp]
        public async Task Given()
        {
            _doneSignal = new TaskCompletionSource<object>();

            _handler.Handle(Arg.Any<GenericMessage>())
                .Returns(true)
                .AndDoes(_ =>
                {
                    Tasks.DelaySendDone(_doneSignal);
                    throw new TestException("My Ex");
                });

            _globalErrorHandler = (ex, m) => { _handledException = true; };
            _monitoring = Substitute.For<IMessageMonitor>();
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

            bus.StartListening();

            bus.Publish(new GenericMessage());
            await _doneSignal.Task;

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
