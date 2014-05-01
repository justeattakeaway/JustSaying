using System;
using Amazon;
using JustEat.Testing;
using JustSaying.Messaging.MessageHandling;
using JustSaying.Messaging.Monitoring;
using JustSaying.TestingFramework;
using JustSaying.Tests.MessageStubs;
using NSubstitute;
using NUnit.Framework;

namespace JustSaying.IntegrationTests.JustSayingFluently
{
    [TestFixture]
    public class WhenAHandlerThrowsAnException
    {
        private readonly IHandler<GenericMessage> _handler = Substitute.For<IHandler<GenericMessage>>();
        private IAmJustSayingFluently _bus;
        private Action<Exception> _globalErrorHandler;
        private bool _handledException;
        private IMessageMonitor _monitoring;

        [TestFixtureSetUp]
        public void Given()
        {
            _handler.Handle(Arg.Any<GenericMessage>()).Returns(true).AndDoes(ex => { throw new Exception("My Ex"); });
            _globalErrorHandler = ex => { _handledException = true; };
            _monitoring = Substitute.For<IMessageMonitor>();
            var bus =  CreateMeABus.InRegion(RegionEndpoint.EUWest1.SystemName).ConfigurePublisherWith(c =>
                                                                        {
                                                                            c.PublishFailureBackoffMilliseconds = 1;
                                                                            c.PublishFailureReAttempts = 3;
                                                                        })
                                                                        .WithMonitoring(_monitoring)
                .WithSnsMessagePublisher<GenericMessage>("CustomerCommunication")
                .WithSqsTopicSubscriber("CustomerCommunication")
                .IntoQueue("queuename").ConfigureSubscriptionWith(cfg =>
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
        public void MessagePopsOutAtTheOtherEnd()
        {
            Patiently.VerifyExpectation(() => _handler.Received().Handle(Arg.Any<GenericMessage>()));
        }

        [Then]
        public void CustomExceptionHandlingIsCalled()
        {
            Patiently.AssertThat(() => _handledException == true);
        }

        [TearDown]
        public void ByeBye()
        {
            _bus.StopListening();
            _bus = null;
        }
    }
}
