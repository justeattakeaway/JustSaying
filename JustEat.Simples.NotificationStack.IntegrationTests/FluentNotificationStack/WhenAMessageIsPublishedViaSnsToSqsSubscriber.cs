using System.Threading;
using JustEat.Simples.NotificationStack.Messaging.MessageHandling;
using JustEat.Simples.NotificationStack.Messaging.Messages;
using NSubstitute;
using NUnit.Framework;
using Tests.MessageStubs;

namespace NotificationStack.IntegrationTests.FluentNotificationStack
{
    [TestFixture]
    public class WhenAMessageIsPublishedViaSnsToSqsSubscriber
    {
        private readonly IHandler<GenericMessage> _handler = Substitute.For<IHandler<GenericMessage>>();
        private JustEat.Simples.NotificationStack.Stack.FluentNotificationStack _publisher;

        [SetUp]
        public void Given()
        {
            _handler.Handle(Arg.Any<GenericMessage>()).Returns(true);

            var publisher = JustEat.Simples.NotificationStack.Stack.FluentNotificationStack.Register(c =>
                                                                        {
                                                                            c.Component = "OrderEngine";
                                                                            c.Tenant = "uk";
                                                                            c.Environment = "integrationTest";
                                                                            c.PublishFailureBackoffMilliseconds = 1;
                                                                            c.PublishFailureReAttempts = 3;
                                                                        })
                .WithSnsMessagePublisher<GenericMessage>("CustomerCommunication")
                .WithSqsTopicSubscriber("CustomerCommunication", 60)
                .WithMessageHandler(_handler);

            publisher.StartListening();
            _publisher = publisher;
        }

        [Test]
        public void ThenItGetsHandled()
        {
            _publisher.Publish(new GenericMessage());
            Thread.Sleep(500);
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
