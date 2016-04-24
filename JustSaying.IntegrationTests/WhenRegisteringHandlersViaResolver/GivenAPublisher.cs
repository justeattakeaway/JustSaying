using System.Threading.Tasks;
using JustBehave;
using JustSaying.Messaging;

namespace JustSaying.IntegrationTests.WhenRegisteringHandlersViaResolver
{
    public abstract class GivenAPublisher : AsyncBehaviourTest<IMessagePublisher>
    {
        protected IHaveFulfilledPublishRequirements Publisher;

        protected Task DoneSignal;

        protected override IMessagePublisher CreateSystemUnderTest()
        {
            Publisher = JustSaying.CreateMeABus.InRegion("eu-west-1")
                .WithSnsMessagePublisher<OrderPlaced>();
            Publisher.StartListening();
            return Publisher;
        }

        protected override async Task When()
        {
            Publisher.Publish(new OrderPlaced("1234"));

            if (DoneSignal != null)
            {
                await DoneSignal;
            }
        }
    }
}