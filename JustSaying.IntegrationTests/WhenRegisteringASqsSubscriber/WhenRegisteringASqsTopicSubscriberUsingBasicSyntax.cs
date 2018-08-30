using System.Threading.Tasks;
using JustSaying.Messaging.MessageHandling;
using JustSaying.Models;
using NSubstitute;

namespace JustSaying.IntegrationTests.WhenRegisteringASqsSubscriber
{
    public class WhenRegisteringASqsTopicSubscriberUsingBasicSyntax : WhenRegisteringASqsTopicSubscriber
    {
        protected override Task When()
        {
            SystemUnderTest.WithSqsTopicSubscriber()
                .IntoQueue(QueueName)
                .WithMessageHandler(Substitute.For<IHandlerAsync<Message>>());

            return Task.CompletedTask;
        }
    }
}
