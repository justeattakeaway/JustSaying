using System;
using System.Threading;
using JustSaying;
using JustSaying.Messaging.MessageHandling;
using JustSaying.Messaging.Monitoring;
using JustSaying.Stack;
using JustEat.Testing;
using JustSaying.Tests.MessageStubs;
using NSubstitute;
using NUnit.Framework;

namespace NotificationStack.IntegrationTests.FluentNotificationStackTests
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
            var publisher = FluentNotificationStack.Register(c =>
                                                                        {
                                                                            c.Component = "TestHarnessExceptions";
                                                                            c.Tenant = "Wherever";
                                                                            c.Environment = "integration";
                                                                            c.PublishFailureBackoffMilliseconds = 1;
                                                                            c.PublishFailureReAttempts = 3;
                                                                        })
                                                                        .WithMonitoring(_monitoring)
                .ConfigurePublisherWith(_=>{})                                                        
                .WithSnsMessagePublisher<GenericMessage>("CustomerCommunication")
                .WithSqsTopicSubscriber("CustomerCommunication")
                .IntoQueue("CustomerCommunication")
                .ConfigureSubscriptionWith(conf =>
                {
                    conf.InstancePosition = 1;
                    conf.OnError = _globalErrorHandler;
                })
                .WithMessageHandler(_handler);

            publisher.StartListening();
            _publisher = publisher;
        }

        [Then]
        public void CustomExceptionHandlingIsCalled()
        {
            _publisher.Publish(new GenericMessage());
            Thread.Sleep(1000);

            _handler.Received().Handle(Arg.Any<GenericMessage>());
            Assert.That(_handledException, Is.EqualTo(true));
        }

        [TearDown]
        public void ByeBye()
        {
            _publisher.StopListening();
            _publisher = null;
        }
    }
}
