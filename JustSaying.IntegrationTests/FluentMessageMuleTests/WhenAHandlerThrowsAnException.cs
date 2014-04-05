using System;
using Amazon;
using JustEat.Testing;
using JustSaying.Messaging.MessageHandling;
using JustSaying.Messaging.Monitoring;
using JustSaying.TestingFramework;
using JustSaying.Tests.MessageStubs;
using NSubstitute;
using NUnit.Framework;

namespace JustSaying.IntegrationTests.FluentMessageMuleTests
{
    [TestFixture]
    public class WhenAHandlerThrowsAnException
    {
        private readonly IHandler<GenericMessage> _handler = Substitute.For<IHandler<GenericMessage>>();
        private IAmJustSayingFluently _publisher;
        private Action<Exception> _globalErrorHandler;
        private bool _handledException;
        private IMessageMonitor _monitoring;

        [SetUp]
        public void Given()
        {
            _handler.Handle(Arg.Any<GenericMessage>()).Returns(true).AndDoes(ex => { throw new Exception("My Ex"); });
            _globalErrorHandler = ex => { _handledException = true; };
            _monitoring = Substitute.For<IMessageMonitor>();
            var publisher =  CreateMe.ABus(c =>
                                                                        {
                                                                            c.PublishFailureBackoffMilliseconds = 1;
                                                                            c.PublishFailureReAttempts = 3;
                                                                            c.Region = RegionEndpoint.EUWest1.SystemName;
                                                                        })
                                                                        .WithMonitoring(_monitoring)
                .WithSnsMessagePublisher<GenericMessage>("CustomerCommunication")
                .WithSqsTopicSubscriber("CustomerCommunication", 60, instancePosition: 1, onError: _globalErrorHandler)
                .WithMessageHandler(_handler);

            publisher.StartListening();
            _publisher = publisher;
        }

        [Then]
        public void CustomExceptionHandlingIsCalled()
        {
            _publisher.Publish(new GenericMessage());

            Patiently.VerifyExpectation(() => _handler.Received().Handle(Arg.Any<GenericMessage>()));
            Patiently.AssertThat(() => _handledException == true);
        }

        [TearDown]
        public void ByeBye()
        {
            _publisher.StopListening();
            _publisher = null;
        }
    }
}
