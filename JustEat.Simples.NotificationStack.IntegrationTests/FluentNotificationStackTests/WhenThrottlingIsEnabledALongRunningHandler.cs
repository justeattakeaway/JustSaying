using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using JustEat.Simples.NotificationStack.Messaging.MessageHandling;
using JustEat.Simples.NotificationStack.Messaging.Monitoring;
using NSubstitute;
using NUnit.Framework;
using Tests.MessageStubs;

namespace NotificationStack.IntegrationTests.FluentNotificationStackTests
{
    [TestFixture]
    public class WhenThrottlingIsEnabledALongRunningHandler
    {
        private readonly IHandler<GenericMessage> _handler = Substitute.For<IHandler<GenericMessage>>();
        private JustEat.Simples.NotificationStack.Stack.IFluentNotificationStack _publisher;
        private readonly Dictionary<int, Guid> _ids = new Dictionary<int, Guid>();
        private readonly Dictionary<int, GenericMessage> _messages = new Dictionary<int, GenericMessage>();

        [SetUp]
        public void Given()
        {
            Enumerable.Range(1, 100).ToList().ForEach(i =>
            {
                _ids.Add(i, Guid.NewGuid());
                _messages.Add(i, new GenericMessage() { Id = _ids[i] });

                //First handler takes ages all the others take 100 ms
                SetUpHandler(_ids[i], i, wait: i == 1 ? 3600000 : 100);
            });



            var publisher = JustEat.Simples.NotificationStack.Stack.FluentNotificationStack.Register(c =>
            {
                c.Component = "TestHarness";
                c.Tenant = "Wherever";
                c.Environment = "integration";
                c.PublishFailureBackoffMilliseconds = 1;
                c.PublishFailureReAttempts = 3;
            })
                                                                        .WithMonitoring(Substitute.For<IMessageMonitor>())
                .WithSnsMessagePublisher<GenericMessage>("CustomerCommunication")
                .WithSqsTopicSubscriber("CustomerCommunication", 60, instancePosition: 1, maxAllowedMessagesInFlight: 25)
                .WithMessageHandler(_handler);

            publisher.StartListening();
            _publisher = publisher;
        }

        private void SetUpHandler(Guid guid, int number, int wait)
        {
            _handler.When(x => x.Handle(Arg.Is<GenericMessage>(y => y.Id == guid))).Do(t =>
            {
                Console.WriteLine("Running task {0}", number);
                Thread.Sleep(wait);
            });
        }

        [Test]
        public void ThenItGetsHandled()
        {
            //Publish the message with a long running handler
            _publisher.Publish(_messages[1]);

            //Give some time to AWS to schedule the first long running message
            Thread.Sleep(2000);

            //publish the rest of the messages except the last one. 
            Enumerable.Range(2, 98).ToList().ForEach(i => _publisher.Publish(_messages[i]));

            //publish the last message after a couple of seconds to guaranty it was scheduled after all the rest
            Thread.Sleep(2000);
            _publisher.Publish(_messages[100]);

            //Wait for a reasonble time before asserting whether the last message has been scheduled.
            //There are 100 messages and all except one takes 100 ms. Therefore, 20 seconds is sufficiently long.
            Thread.Sleep(20000);

            _handler.Received().Handle(Arg.Is<GenericMessage>(x => x.Id == _ids[100]));

        }

        [TearDown]
        public void ByeBye()
        {
            _publisher.StopListening();
            _publisher = null;
        }
    }
}