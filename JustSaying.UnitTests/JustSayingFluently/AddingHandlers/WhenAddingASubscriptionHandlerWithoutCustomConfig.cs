using JustBehave;
using JustSaying.Messaging.MessageHandling;
using JustSaying.Models;
using NSubstitute;

namespace JustSaying.UnitTests.JustSayingFluently.AddingHandlers
{
    public class WhenAddingASubscriptionHandlerWithoutCustomConfig : JustSayingFluentlyTestBase
    {
        private readonly IHandler<Message> _handler = Substitute.For<IHandler<Message>>();
        private IFluentSubscription _bus;

        protected override void Given() { }

        protected override void When()
        {
            _bus = SystemUnderTest
                .WithSqsTopicSubscriber()
                .IntoQueue("queuename");
        }

        [Then]
        public void ConfigurationIsNotRequired()
        {
            // Tested by the fact that handlers can be added
            _bus.WithMessageHandler(_handler)
                .WithMessageHandler(_handler);
        }

        [Then]
        public void ConfigurationCanBeProvided()
        {
            _bus.ConfigureSubscriptionWith(conf => conf.InstancePosition = 1);
        }
    }
}