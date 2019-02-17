using System.Threading.Tasks;
using JustSaying.AwsTools.QueueCreation;
using JustSaying.Messaging.MessageHandling;
using JustSaying.Messaging.MessageSerialization;
using JustSaying.Models;
using Microsoft.Extensions.DependencyInjection;
using NSubstitute;
using Xunit;
using Xunit.Abstractions;

namespace JustSaying.UnitTests.JustSayingFluently.AddingHandlers
{
    public class WhenAddingASubscriptionHandler : UnitTestBase
    {
        private readonly IHandlerAsync<Message> _handler = Substitute.For<IHandlerAsync<Message>>();

        public WhenAddingASubscriptionHandler(ITestOutputHelper outputHelper) : base(outputHelper)
        { }

        protected override void ConfigureServices(IServiceCollection services)
        {
            base.ConfigureServices(services);
            services.AddSingleton(_handler);
        }

        protected override void ConfigureJustSaying(MessagingBusBuilder builder)
        {
            base.ConfigureJustSaying(builder);
            builder.Subscriptions(
                (options) => options.ForTopic<Message>());
        }

        protected override Task WhenAsync()
        {
            var messageBus = Services.GetService<IMessagingBus>();
            return Task.CompletedTask;
        }

        [Fact]
        public void TheTopicAndQueueIsCreatedInEachRegion()
        {
            QueueVerifier.Received()
                .EnsureTopicExistsWithQueueSubscribedAsync(
                    "defaultRegion",
                    Arg.Any<IMessageSerializationRegister>(),
                    Arg.Any<SqsReadConfiguration>(),
                    Arg.Any<IMessageSubjectProvider>());

            QueueVerifier.Received()
                .EnsureTopicExistsWithQueueSubscribedAsync(
                    "failoverRegion",
                    Arg.Any<IMessageSerializationRegister>(),
                    Arg.Any<SqsReadConfiguration>(),
                    Arg.Any<IMessageSubjectProvider>());
        }

        [Fact]
        public void TheSubscriptionIsCreatedInEachRegion()
        {
            //Bus.Received(2).AddNotificationSubscriber(Arg.Any<string>(), Arg.Any<INotificationSubscriber>());
        }

        [Fact]
        public void HandlerIsAddedToBus()
        {
            //Bus.Received().AddMessageHandler(Arg.Any<string>(), Arg.Any<string>(), Arg.Any<Func<IHandlerAsync<Message>>>());
        }

        [Fact]
        public void SerializationIsRegisteredForMessage()
        {
            // Bus.SerializationRegister.Received().AddSerializer<Message>(Arg.Any<IMessageSerializer>());
        }
    }
}

