using System.Threading;
using JustEat.Simples.NotificationStack.Messaging.MessageHandling;
using JustEat.Simples.NotificationStack.Messaging.Messages;
using JustEat.Simples.NotificationStack.Messaging.Monitoring;
using NSubstitute;
using NUnit.Framework;
using Tests.MessageStubs;

namespace NotificationStack.IntegrationTests.FluentNotificationStack
{
    [TestFixture]
    public class WhenAMessageIsPublishedViaSnsToSqsSubscriber
    {
        private readonly IHandler<GenericMessage> _handler = Substitute.For<IHandler<GenericMessage>>();
        private JustEat.Simples.NotificationStack.Stack.IFluentNotificationStack _publisher;

        [SetUp]
        public void Given()
        {
            _handler.Handle(Arg.Any<GenericMessage>()).Returns(true);

            var publisher = JustEat.Simples.NotificationStack.Stack.FluentNotificationStack.Register(c =>
                                                                        {
                                                                            c.Component = "TestHarnessHandling";
                                                                            c.Tenant = "Wherever";
                                                                            c.Environment = "integration";
                                                                            c.PublishFailureBackoffMilliseconds = 1;
                                                                            c.PublishFailureReAttempts = 3;
                                                                        })
                                                                        .WithMonitoring(Substitute.For<IMessageMonitor>())
                .WithSnsMessagePublisher<GenericMessage>("CustomerCommunication")
                .WithSqsTopicSubscriber("CustomerCommunication", 60, instancePosition: 1)
                .WithMessageHandler(_handler);

            publisher.StartListening();
            _publisher = publisher;
        }

        [Test]
        public void ThenItGetsHandled()
        {
            _publisher.Publish(new GenericMessage());
            Thread.Sleep(2000);

            _handler.Received().Handle(Arg.Any<GenericMessage>());
        }

        [TearDown]
        public void ByeBye()
        {
            _publisher.StopListening();
            _publisher = null;
        }
    }
}
