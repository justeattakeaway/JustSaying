using JustSaying.Messaging.MessageHandling;
using JustSaying.Models;
using NSubstitute;

namespace JustSaying.IntegrationTests.WhenRegisteringASqsSubscriber
{
    public class WhenRegisteringLongNameMessageTypeTopicSubscriber
    {
        public class LongLongLongLongLonggLongLongLongLongLongLongLongLongLongLongLongLongLongLongLongLongLongLongLongLongLongLongLongLongLongLongLongLongLongLongLongLonggLongLongLongLongLongLongLongLongLongLongLongLongLongLongLongLongLongLongLongLongLongLongLongLongLongMessage : Message
        {
        }

        public class WhenRegisteringASqsGenericMessageTopicSubscriber : WhenRegisteringASqsTopicSubscriber
        {
            protected override void When()
            {
                SystemUnderTest.WithSqsTopicSubscriber()
                    .IntoQueueNamed(QueueName)
                    .WithMessageHandler(Substitute.For<IHandlerAsync<LongLongLongLongLonggLongLongLongLongLongLongLongLongLongLongLongLongLongLongLongLongLongLongLongLongLongLongLongLongLongLongLongLongLongLongLongLonggLongLongLongLongLongLongLongLongLongLongLongLongLongLongLongLongLongLongLongLongLongLongLongLongLongMessage>>());
            }
        } 
    }
}