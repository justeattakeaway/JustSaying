using JustSaying.Fluent;
using JustSaying.Messaging;
using JustSaying.Messaging.MessageHandling;
using JustSaying.Messaging.Middleware;
using JustSaying.Messaging.Middleware.Logging;
using JustSaying.Messaging.Middleware.PostProcessing;
using JustSaying.TestingFramework;
using LocalSqsSnsMessaging;
using Microsoft.Extensions.Logging;
using Shouldly;
using StructureMap;
using TUnit.Core;

namespace JustSaying;

public class WhenUsingStructureMap
{
    private InMemoryAwsBus InMemoryAwsBus { get; } = new();

    [Test]
    public async Task Can_Create_Messaging_Bus_Fluently_For_A_Queue()
    {
        // Arrange
        var outputWriter = TestContext.Current!.OutputWriter;
        var handler = new InspectableHandler<SimpleMessage>();

        using var container = new Container(
            (registry) =>
            {
                registry.For<ILoggerFactory>()
                    .Use(() => outputWriter.ToLoggerFactory())
                    .Singleton();

                registry.For<IHandlerAsync<SimpleMessage>>()
                    .Use(handler);

                registry.AddJustSaying(
                    (builder) =>
                    {
                        builder.Client((options) =>
                                    options.WithClientFactory(() => new LocalAwsClientFactory(InMemoryAwsBus))
                                )
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

        await Patiently.AssertThatAsync(outputWriter, () => handler.ReceivedMessages.Count > 1);

        // Assert
        handler.ReceivedMessages.ShouldContain(x => x.GetType() == typeof(SimpleMessage));
    }

    [Test]
    public void Registers_Middleware_With_The_Correct_Lifetime()
    {
        // Arrange
        var outputWriter = TestContext.Current!.OutputWriter;
        using var container = new Container(
            (registry) =>
            {
                registry.For<ILoggerFactory>()
                    .Use(() => outputWriter.ToLoggerFactory())
                    .Singleton();

                registry.AddJustSaying(
                    (builder) =>
                    {
                        builder.Client((options) =>
                                options.WithBasicCredentials("accessKey", "secretKey")
                                    .WithServiceUri(TestEnvironment.SimulatorUrl))
                            .Messaging((options) => options.WithRegion("eu-west-1"));
                    });
            });

        var handlerMiddlewareBuilder = new HandlerMiddlewareBuilder(container.GetInstance<IHandlerResolver>(), container.GetInstance<IServiceResolver>());
        var useLoggingMiddlewareTwice = () => UseMiddlewareTwice<LoggingMiddleware>();
        var useSqsPostProcessorMiddlewareTwice = () => UseMiddlewareTwice<SqsPostProcessorMiddleware>();

        void UseMiddlewareTwice<T>()
            where T : MiddlewareBase<HandleMessageContext, bool>
        {
            handlerMiddlewareBuilder.Use<LoggingMiddleware>();
            handlerMiddlewareBuilder.Use<LoggingMiddleware>();
        }

        // Act + Assert
        useLoggingMiddlewareTwice.ShouldNotThrow();
        useSqsPostProcessorMiddlewareTwice.ShouldNotThrow();
    }
}
