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
        protected Future<AnotherGenericMessage> _snsHandler2;


        protected override void Given()
        {
            _snsHandler1 = new Future<GenericMessage>();
            _snsHandler2 = new Future<AnotherGenericMessage>();

            var snsHandler1 = Substitute.For<IHandler<GenericMessage>>();
            snsHandler1.When(x => x.Handle(Arg.Any<GenericMessage>()))
                    .Do(x => _snsHandler1.Complete((GenericMessage)x.Args()[0]));

            var snsHandler2 = Substitute.For<IHandler<AnotherGenericMessage>>();
            snsHandler2.When(x => x.Handle(Arg.Any<AnotherGenericMessage>()))
                    .Do(x => _snsHandler2.Complete((AnotherGenericMessage)x.Args()[0]));

            ServiceBus = CreateMeABus.InRegion(RegionEndpoint.EUWest1.SystemName)
                .ConfigurePublisherWith(c => c.TopicNameProvider = t => "Test")

                .WithSnsMessagePublisher<GenericMessage>()
                .WithSqsTopicSubscriber()
                .IntoQueue("queuename")
                .ConfigureSubscriptionWith(cf => cf.TopicNameProvider = t => "Test")
                .WithMessageHandler(snsHandler1)

                .WithSnsMessagePublisher<GenericMessage>()
                .WithSqsTopicSubscriber()
                .IntoQueue("queuename")
                .ConfigureSubscriptionWith(cf => cf.TopicNameProvider = t => "Test1")
                .WithMessageHandler(snsHandler2);

            ServiceBus.StartListening();

        }

        protected override void When()
        {
            ServiceBus.Publish(new GenericMessage());
        }

        [Test]
        public void ThenItGetsHandledInHandler1()
        {
            _snsHandler1.WaitUntilCompletion(10.Seconds()).ShouldBe(true);
        }

        [Test]
        public void ThenItDoesntGetsHandledInHandler2()
        {
            _snsHandler2.WaitUntilCompletion(10.Seconds()).ShouldBe(false);
        }
    }
}