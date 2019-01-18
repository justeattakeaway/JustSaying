using System.Threading.Tasks;
using JustSaying.Messaging.MessageHandling;
using JustSaying.Models;
using NSubstitute;

namespace JustSaying.IntegrationTests.WhenRegisteringASqsSubscriber
{
// disable warning about the pathological class names and class nesting in this test
#pragma warning disable CA1052
    public class WhenRegisteringLongNameMessageTypeTopicSubscriber
#pragma warning restore CA1052
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
