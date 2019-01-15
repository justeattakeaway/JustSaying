using System.Threading.Tasks;
using JustSaying.Messaging.MessageHandling;
using JustSaying.Models;
using NSubstitute;

namespace JustSaying.IntegrationTests.WhenRegisteringASqsSubscriber
{
#pragma warning disable CA1052
#pragma warning disable CA1034
    public class WhenRegisteringLongNameMessageTypeTopicSubscriber
    {
        public class LongLongLongLongLonggLongLongLongLongLongLongLongLongLongLongLongLongLongLongLongLongLongLongLongLongLongLongLongLongLongLongLongLongLongLongLongLonggLongLongLongLongLongLongLongLongLongLongLongLongLongLongLongLongLongLongLongLongLongLongLongLongLongMessage : Message
        {
        }

        public class WhenRegisteringASqsGenericMessageTopicSubscriber : WhenRegisteringASqsTopicSubscriber
        {
            protected override Task When()
            {
                SystemUnderTest.WithSqsTopicSubscriber()
                    .IntoQueue(QueueName)
                    .WithMessageHandler(Substitute.For<IHandlerAsync<LongLongLongLongLonggLongLongLongLongLongLongLongLongLongLongLongLongLongLongLongLongLongLongLongLongLongLongLongLongLongLongLongLongLongLongLongLonggLongLongLongLongLongLongLongLongLongLongLongLongLongLongLongLongLongLongLongLongLongLongLongLongLongMessage>>());

                return Task.CompletedTask;
            }
        } 
    }
}
