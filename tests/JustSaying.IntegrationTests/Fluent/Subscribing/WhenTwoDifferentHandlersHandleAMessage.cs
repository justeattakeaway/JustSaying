using System.Threading.Tasks;
using JustSaying.TestingFramework;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging.Abstractions;
using Shouldly;
using Xunit.Abstractions;

namespace JustSaying.IntegrationTests.Fluent.Subscribing
{
    public class WhenTwoDifferentHandlersHandleAMessage : IntegrationTestBase
    {
        public WhenTwoDifferentHandlersHandleAMessage(ITestOutputHelper outputHelper)
            : base(outputHelper)
        {
        }

        [AwsFact]
        public async Task Then_Both_Handlers_Receive_The_Message()
        {
            // Arrange
            var nullStoreLogger = NullLogger<TestMessageStore<SimpleMessage>>.Instance;
            var nullHandlerLogger = NullLogger<MessageStoringHandler<SimpleMessage>>.Instance;

            var handler1 = new MessageStoringHandler<SimpleMessage>(new TestMessageStore<SimpleMessage>(nullStoreLogger), nullHandlerLogger);
            var handler2 = new MessageStoringHandler<SimpleMessage>(new TestMessageStore<SimpleMessage>(nullStoreLogger), nullHandlerLogger);

            var services = GivenJustSaying()
                .ConfigureJustSaying((builder) => builder.WithLoopbackTopic<SimpleMessage>(UniqueName))
                .AddJustSayingHandlers(new[] { handler1, handler2 });

            await WhenAsync(
                services,
                async (publisher, listener, serviceProvider, cancellationToken) =>
                {
                    await listener.StartAsync(cancellationToken);
                    await publisher.StartAsync(cancellationToken);

                    var message = new SimpleMessage();

                    // Act
                    await publisher.PublishAsync(message, cancellationToken);
                    await Task.Delay(1.Seconds(), cancellationToken);

                    // Assert
                    handler1.MessageStore.Messages.ShouldHaveSingleItem().UniqueKey().ShouldBe(message.UniqueKey());
                    handler2.MessageStore.Messages.ShouldHaveSingleItem().UniqueKey().ShouldBe(message.UniqueKey());
                });
        }
    }
}
