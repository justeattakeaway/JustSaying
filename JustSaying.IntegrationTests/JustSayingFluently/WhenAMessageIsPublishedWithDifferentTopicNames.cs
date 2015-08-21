using Amazon;
using JustSaying.Messaging.MessageHandling;
using JustSaying.TestingFramework;
using NSubstitute;
using NUnit.Framework;
using Shouldly;

namespace JustSaying.IntegrationTests.JustSayingFluently
{
    public class WhenAMessageIsPublishedWithDifferentTopicNames : GivenANotificationStack
    {
        private Future<GenericMessage> _snsHandler1;
        protected Future<GenericMessage> _snsHandler2;

        private IAmJustSayingFluently _bus1;
        protected override void Given()
        {
            _snsHandler1 = new Future<GenericMessage>();
            _snsHandler2 = new Future<GenericMessage>();

            var snsHandler1 = Substitute.For<IHandler<GenericMessage>>();
            snsHandler1.When(x => x.Handle(Arg.Any<GenericMessage>()))
                    .Do(x => _snsHandler1.Complete((GenericMessage)x.Args()[0]));

            var snsHandler2 = Substitute.For<IHandler<GenericMessage>>();
            snsHandler2.When(x => x.Handle(Arg.Any<GenericMessage>()))
                    .Do(x => _snsHandler2.Complete((GenericMessage)x.Args()[0]));

            ServiceBus = CreateMeABus.InRegion(RegionEndpoint.EUWest1.SystemName)
                .ConfigurePublisherWith(c => c.TopicNameProvider = t => "Environment1")
                .WithSnsMessagePublisher<GenericMessage>()
                .WithSqsTopicSubscriber()
                .IntoQueue("queuename")
                .WithMessageHandler(snsHandler1);

            _bus1 = CreateMeABus.InRegion(RegionEndpoint.EUWest1.SystemName)
                .ConfigurePublisherWith(c => c.TopicNameProvider = t => "Environment2")
                .WithSnsMessagePublisher<GenericMessage>()
                .WithSqsTopicSubscriber()
                .IntoQueue("queuename1")
                .WithMessageHandler(snsHandler2);

            _bus1.StartListening();

            ServiceBus.StartListening();

        }

        protected override void When()
        {
            ServiceBus.Publish(new GenericMessage());
        }

        public override void PostAssertTeardown()
        {
            base.PostAssertTeardown();
            _bus1.StopListening();

        }

        [Test]
        public void ThenItGetsHandledInHandler1()
        {
            _snsHandler1.WaitUntilCompletion(10.Seconds()).ShouldBe(true);
        }

        [Test]
        public void ThenItDoesntGetHandledInHandler2()
        {
            _snsHandler2.WaitUntilCompletion(10.Seconds()).ShouldBe(false);
        }
    }
}