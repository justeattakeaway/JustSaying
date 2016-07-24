using JustSaying.Messaging.MessageHandling;
using JustSaying.Models;
using NSubstitute;

namespace JustSaying.IntegrationTests.WhenRegisteringASqsSubscriber
{
    public class GenericMessage<T> : Message
    {
        public T Contents { get; set; }
    }

    public class MyMessage { }

    public class WhenRegisteringASqsGenericMessageTopicSubscriber : WhenRegisteringASqsTopicSubscriber
    {
        protected override void When()
        {
            SystemUnderTest.WithSqsTopicSubscriber()
                .IntoQueueNamed(QueueName)
                .WithMessageHandler(Substitute.For<IHandlerAsync<GenericMessage<MyMessage>>>());
        }
    }
}