using System;
using System.Threading.Tasks;
using JustSaying.AwsTools.QueueCreation;
using JustSaying.Messaging;
using JustSaying.Messaging.MessageHandling;
using JustSaying.Messaging.MessageSerialization;
using JustSaying.TestingFramework;
using NSubstitute;
using Shouldly;
using Xunit;

namespace JustSaying.UnitTests.JustSayingFluently.AddingHandlers
{
    public class WhenAddingASubscriptionHandler : JustSayingFluentlyTestBase
    {
        private readonly IHandlerAsync<Message> _handler = Substitute.For<IHandlerAsync<Message>>();
        private object _response;

        protected override void Given()
        {
            HandlerResolver.ResolveHandler<Message>(new HandlerResolutionContext("queue-name")).Returns(_handler);
        }

        protected override Task WhenAsync()
        {
            _response = SystemUnderTest
                .WithSqsTopicSubscriber()
                .IntoDefaultQueue()
                .WithMessageHandler<Message>(HandlerResolver);

            return Task.CompletedTask;
        }

        [Fact]
        public void TheTopicAndQueueIsCreatedInEachRegion()
        {
            QueueVerifier.Received().EnsureTopicExistsWithQueueSubscribedAsync("defaultRegion", Bus.SerializationRegister, Arg.Any<SqsReadConfiguration>(), Bus.Config.MessageSubjectProvider);
            QueueVerifier.Received().EnsureTopicExistsWithQueueSubscribedAsync("failoverRegion", Bus.SerializationRegister, Arg.Any<SqsReadConfiguration>(), Bus.Config.MessageSubjectProvider);
        }

        [Fact]
        public void TheSubscriptionIsCreatedInEachRegion()
        {
            Bus.Received(2).AddNotificationSubscriber(Arg.Any<string>(), Arg.Any<INotificationSubscriber>());
        }

        [Fact]
        public void HandlerIsAddedToBus()
        {
            Bus.Received().AddMessageHandler(Arg.Any<string>(), Arg.Any<string>(), Arg.Any<Func<IHandlerAsync<Message>>>());
        }

        [Fact]
        public void SerializationIsRegisteredForMessage()
        {
            Bus.SerializationRegister.Received().AddSerializer<Message>(Arg.Any<IMessageSerializer>());
        }

        [Fact]
        public void ICanContinueConfiguringTheBus()
        {
            _response.ShouldBeAssignableTo<IFluentSubscription>();
        }
    }
}
