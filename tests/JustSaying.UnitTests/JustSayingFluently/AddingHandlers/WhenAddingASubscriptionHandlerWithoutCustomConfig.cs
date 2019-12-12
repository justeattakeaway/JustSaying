using System.Threading.Tasks;
using JustSaying.Messaging.MessageHandling;
using JustSaying.Models;
using NSubstitute;
using Xunit;

namespace JustSaying.UnitTests.JustSayingFluently.AddingHandlers
{
    public class WhenAddingASubscriptionHandlerWithoutCustomConfig : JustSayingFluentlyTestBase
    {
        private readonly IHandlerAsync<Message> _handler = Substitute.For<IHandlerAsync<Message>>();
        private IFluentSubscription _bus;

        protected override Task WhenAsync()
        {
            _bus = SystemUnderTest
                .WithSqsTopicSubscriber()
                .IntoQueue("queuename");

            return Task.CompletedTask;
        }

        [Fact]
        public void ConfigurationIsNotRequired()
        {
            // Tested by the fact that handlers can be added
            _bus.WithMessageHandler(_handler)
                .WithMessageHandler(_handler);
        }
    }
}
