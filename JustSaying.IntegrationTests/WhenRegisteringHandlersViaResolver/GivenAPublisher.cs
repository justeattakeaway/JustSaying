using JustBehave;
using JustSaying.Messaging;

namespace JustSaying.IntegrationTests.WhenRegisteringHandlersViaResolver
{
    public abstract class GivenAPublisher : BehaviourTest<IMessagePublisher>
    {
        protected IHaveFulfilledPublishRequirements Publisher;

        protected override IMessagePublisher CreateSystemUnderTest()
        {
            Publisher = JustSaying.CreateMeABus.InRegion("eu-west-1")
                .WithSnsMessagePublisher<OrderPlaced>();
            Publisher.StartListening();
            return Publisher;
        }

        protected override void When()
        {
            Publisher.Publish(new OrderPlaced("1234"));
        }
    }
}