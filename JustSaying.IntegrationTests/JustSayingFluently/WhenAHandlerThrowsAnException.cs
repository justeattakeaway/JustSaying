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
        private IAmJustSayingFluently _bus;
        private Action<Exception, Amazon.SQS.Model.Message> _globalErrorHandler;
        private bool _handledException;
        private IMessageMonitor _monitoring;

        [TestFixtureSetUp]
        public void Given()
        {
            _handler.Handle(Arg.Any<GenericMessage>()).Returns(true).AndDoes(ex => { throw new Exception("My Ex"); });
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
            _bus = bus;
        }

        [SetUp]
        public void When()
        {
            _bus.Publish(new GenericMessage());
        }

        [Then]
        public async Task MessagePopsOutAtTheOtherEnd()
        {
            await Patiently.VerifyExpectationAsync(
                () => _handler.Received().Handle(Arg.Any<GenericMessage>()));
        }

        [Then]
        public async Task CustomExceptionHandlingIsCalled()
        {
            await Patiently.AssertThatAsync(() => _handledException);
        }

        [TestFixtureTearDown]
        public void ByeBye()
        {
            _bus.StopListening();
            _bus = null;
        }
    }
}
