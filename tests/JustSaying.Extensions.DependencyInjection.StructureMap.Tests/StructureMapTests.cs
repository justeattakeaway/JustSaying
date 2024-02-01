using JustSaying.Messaging;
using JustSaying.Messaging.MessageHandling;
using JustSaying.TestingFramework;
using Microsoft.Extensions.Logging;
using Shouldly;
using StructureMap;
using Xunit.Abstractions;

namespace JustSaying;

public class WhenUsingStructureMap(ITestOutputHelper outputHelper)
{
    private ITestOutputHelper OutputHelper { get; } = outputHelper;

    [AwsFact]
    public async Task Can_Create_Messaging_Bus_Fluently_For_A_Queue()
    {
        // Arrange
        var handler = new InspectableHandler<SimpleMessage>();

        using var container = new Container(
            (registry) =>
            {
                registry.For<ILoggerFactory>()
                    .Use(() => OutputHelper.ToLoggerFactory())
                    .Singleton();

                registry.For<IHandlerAsync<SimpleMessage>>()
                    .Use(handler);

                registry.AddJustSaying(
                    (builder) =>
                    {
                        builder.Client((options) =>
                                options.WithBasicCredentials("accessKey", "secretKey")
                                    .WithServiceUri(TestEnvironment.SimulatorUrl))
                            .Messaging((options) => options.WithRegion("eu-west-1"))
                            .Publications((options) => options.WithQueue<SimpleMessage>())
                            .Subscriptions((options) => options.ForQueue<SimpleMessage>());
                    });
            });

        var publisher = container.GetInstance<IMessagePublisher>();
        var batchPublisher = container.GetInstance<IMessageBatchPublisher>();
        IMessagingBus listener = container.GetInstance<IMessagingBus>();

        var message = new SimpleMessage();

        using var source = new CancellationTokenSource(TimeSpan.FromSeconds(20));
        await listener.StartAsync(source.Token);
        await publisher.StartAsync(source.Token);

        if (batchPublisher != publisher)
        {
            await batchPublisher.StartAsync(source.Token);
        }

        // Act
        await publisher.PublishAsync(message, source.Token);
        await batchPublisher.PublishAsync([message], source.Token);

        await Patiently.AssertThatAsync(OutputHelper, () => handler.ReceivedMessages.Count > 1);

        // Assert
        handler.ReceivedMessages.ShouldContain(x => x.GetType() == typeof(SimpleMessage));
    }
}
