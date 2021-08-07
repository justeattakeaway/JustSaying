using System.Linq;
using System.Threading.Tasks;
using JustSaying.Fluent;
using JustSaying.Messaging.MessageHandling;
using JustSaying.Messaging.Middleware;
using JustSaying.Messaging.Middleware.ErrorHandling;
using JustSaying.Messaging.Middleware.PostProcessing;
using JustSaying.TestingFramework;
using Microsoft.Extensions.DependencyInjection;
using Newtonsoft.Json;
using Shouldly;
using Xunit.Abstractions;

namespace JustSaying.IntegrationTests.Fluent.Subscribing
{
    public class WhenHandlerIsDeclaredAsExactlyOnce : IntegrationTestBase
    {
        public WhenHandlerIsDeclaredAsExactlyOnce(ITestOutputHelper outputHelper)
            : base(outputHelper)
        {
        }

        [AwsFact]
        public async Task Then_The_Handler_Only_Receives_The_Message_Once()
        {
            // Arrange
            var messageLock = new MessageLockStore();
            var handler = new InspectableHandler<SimpleMessage>();

            var services = GivenJustSaying()
                .AddSingleton<IMessageLockAsync>(messageLock)
                .ConfigureJustSaying((builder) =>
                    builder.WithLoopbackTopic<SimpleMessage>(UniqueName,
                        c => c.WithMiddlewareConfiguration(m =>
                            m.UseExactlyOnce<SimpleMessage>("lock-simple-message")
                                .UseDefaults<SimpleMessage>(handler.GetType()))))
                .AddJustSayingHandlers(new[] { handler });

            await WhenAsync(
                services,
                async (publisher, listener, serviceProvider, cancellationToken) =>
                {
                    await listener.StartAsync(cancellationToken);
                    await publisher.StartAsync(cancellationToken);

                    var message = new SimpleMessage();

                    // Act
                    await publisher.PublishAsync(message, cancellationToken);
                    await publisher.PublishAsync(message, cancellationToken);

                    var json = JsonConvert.SerializeObject(listener.Interrogate(), Formatting.Indented)
                        .Replace(UniqueName, "TestQueueName");
                    json.ShouldMatchApproved(c => c
                        .SubFolder("Approvals")
                        .WithFilenameGenerator(
                            (_, _, type, extension) =>
                                $"{nameof(WhenHandlerIsDeclaredAsExactlyOnce)}.{nameof(Then_The_Handler_Only_Receives_The_Message_Once)}.{type}.{extension}"));

                    await Patiently.AssertThatAsync(() =>
                    {
                        handler.ReceivedMessages.Where(m => m.Id.ToString() == message.UniqueKey())
                            .ShouldHaveSingleItem();
                    });
                });
        }
    }
}
