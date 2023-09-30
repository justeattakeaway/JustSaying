using JustSaying.Fluent;
using JustSaying.Messaging;
using JustSaying.Messaging.Monitoring;
using JustSaying.Models;
using JustSaying.TestingFramework;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;

namespace JustSaying.IntegrationTests;

public class MessagingBusBuilderTests(ITestOutputHelper outputHelper)
{
    private ITestOutputHelper OutputHelper { get; } = outputHelper;

    private class QueueStore(ILogger<TestMessageStore<QueueMessage>> logger) : TestMessageStore<QueueMessage>(logger)
    {
    }

    [AwsFact]
    public async Task Can_Create_Messaging_Bus_Fluently_For_A_Queue()
    {
        var queueName = Guid.NewGuid().ToString();

        // Arrange
        var services = new ServiceCollection()
            .AddLogging((p) => p.AddXUnit(OutputHelper))
            .AddJustSaying(
                (builder) =>
                {
                    builder.Client((options) =>
                            options.WithBasicCredentials("accessKey", "secretKey")
                                .WithServiceUri(TestEnvironment.SimulatorUrl))
                        .Messaging((options) => options.WithRegion("eu-west-1"))
                        .Publications((options) => options.WithQueue<QueueMessage>(o => o.WithName(queueName)))
                        .Subscriptions((options) => options.ForQueue<QueueMessage>(o => o.WithQueueName(queueName)))
                        .Services((options) => options.WithMessageMonitoring(() => new MyMonitor()));
                })
            .AddSingleton<IMessageStore<QueueMessage>, QueueStore>()
            .AddJustSayingHandler<QueueMessage, MessageStoringHandler<QueueMessage>>();

        IServiceProvider serviceProvider = services.BuildServiceProvider();

        IMessagePublisher publisher = serviceProvider.GetRequiredService<IMessagePublisher>();
        IMessagingBus listener = serviceProvider.GetRequiredService<IMessagingBus>();

        using var source = new CancellationTokenSource(TimeSpan.FromSeconds(20));

        // Act
        await listener.StartAsync(source.Token);
        await publisher.StartAsync(source.Token);

        var message = new QueueMessage();

        await publisher.PublishAsync(message, source.Token);

        var store = serviceProvider.GetRequiredService<IMessageStore<QueueMessage>>();

        // Assert
        await Patiently.AssertThatAsync(OutputHelper,
            () => store.Messages.Any(msg => msg.Id == message.Id));
    }

    [AwsFact]
    public async Task Can_Create_Messaging_Bus_Fluently_For_A_Topic()
    {
        var topicName = Guid.NewGuid().ToString();

        // Arrange
        var services = new ServiceCollection()
            .AddLogging((p) => p.AddXUnit(OutputHelper))
            .AddJustSaying(
                (builder) =>
                {
                    builder
                        .Client((options) =>
                            options.WithBasicCredentials("accessKey", "secretKey")
                                .WithServiceUri(TestEnvironment.SimulatorUrl))
                        .Messaging((options) => options.WithRegion("eu-west-1"))
                        .Publications((options) => options.WithTopic<TopicMessage>())
                        .Subscriptions((options) => options.ForTopic<TopicMessage>(cfg => cfg.WithQueueName(topicName)));
                })
            .AddSingleton<IMessageStore<TopicMessage>, TestMessageStore<TopicMessage>>()
            .AddJustSayingHandler<TopicMessage, MessageStoringHandler<TopicMessage>>();

        IServiceProvider serviceProvider = services.BuildServiceProvider();

        IMessagePublisher publisher = serviceProvider.GetRequiredService<IMessagePublisher>();
        IMessagingBus listener = serviceProvider.GetRequiredService<IMessagingBus>();

        using var source = new CancellationTokenSource(TimeSpan.FromSeconds(20));

        // Act
        await listener.StartAsync(source.Token);
        await publisher.StartAsync(source.Token);

        var message = new TopicMessage();

        await publisher.PublishAsync(message, source.Token);

        var store = serviceProvider.GetService<IMessageStore<TopicMessage>>();

        await Patiently.AssertThatAsync(OutputHelper,
            () => store.Messages.Any(msg => msg.Id == message.Id));
    }

    [AwsFact]
    public async Task Can_Create_Messaging_Bus()
    {
        // Arrange
        var services = new ServiceCollection()
            .AddLogging((p) => p.AddXUnit(OutputHelper))
            .AddJustSaying("eu-west-1")
            .AddJustSayingHandler<QueueMessage, InspectableHandler<QueueMessage>>();

        IServiceProvider serviceProvider = services.BuildServiceProvider();

        IMessagePublisher publisher = serviceProvider.GetRequiredService<IMessagePublisher>();
        IMessagingBus listener = serviceProvider.GetRequiredService<IMessagingBus>();

        using var source = new CancellationTokenSource(TimeSpan.FromSeconds(20));

        // Act
        await listener.StartAsync(source.Token);
        await publisher.StartAsync(source.Token);
    }

    [AwsFact]
    public async Task Can_Create_Messaging_Bus_With_Contributors()
    {
        // Arrange
        var services = new ServiceCollection()
            .AddLogging((p) => p.AddXUnit(OutputHelper))
            .AddJustSaying()
            .AddSingleton<IMessageBusConfigurationContributor, AwsContributor>()
            .AddSingleton<IMessageBusConfigurationContributor, MessagingContributor>()
            .AddSingleton<IMessageBusConfigurationContributor, QueueContributor>()
            .AddSingleton<IMessageBusConfigurationContributor, RegionContributor>()
            .AddJustSayingHandler<QueueMessage, MessageStoringHandler<QueueMessage>>()
            .AddSingleton<IMessageStore<QueueMessage>, TestMessageStore<QueueMessage>>()
            .AddSingleton<MyMonitor>();

        IServiceProvider serviceProvider = services.BuildServiceProvider();

        IMessagePublisher publisher = serviceProvider.GetRequiredService<IMessagePublisher>();
        IMessagingBus listener = serviceProvider.GetRequiredService<IMessagingBus>();

        using var source = new CancellationTokenSource(TimeSpan.FromSeconds(20));

        // Act
        await listener.StartAsync(source.Token);
        await publisher.StartAsync(source.Token);

        var message = new QueueMessage();

        await publisher.PublishAsync(message, source.Token);

        // Assert
        var messageStore = serviceProvider.GetService<IMessageStore<QueueMessage>>();

        await Patiently.AssertThatAsync(OutputHelper,
            () =>
            {
                messageStore.Messages.ShouldContain(msg =>
                    msg.Id.Equals(message.Id));
            });
    }

    private sealed class AwsContributor : IMessageBusConfigurationContributor
    {
        public void Configure(MessagingBusBuilder builder)
        {
            builder.Client(
                (options) => options.WithSessionCredentials("accessKeyId", "secretKeyId", "token")
                    .WithServiceUri(TestEnvironment.SimulatorUrl));
        }
    }

    private sealed class MessagingContributor(IServiceProvider serviceProvider) : IMessageBusConfigurationContributor
    {
        private IServiceProvider ServiceProvider { get; } = serviceProvider;

        public void Configure(MessagingBusBuilder builder)
        {
            builder.Services(
                (p) => p.WithMessageMonitoring(ServiceProvider.GetRequiredService<MyMonitor>));
        }
    }

    private sealed class QueueContributor : IMessageBusConfigurationContributor
    {
        public string QueueName { get; } = Guid.NewGuid().ToString();

        public void Configure(MessagingBusBuilder builder)
        {
            builder.Publications((p) => p.WithQueue<QueueMessage>(options => options.WithName(QueueName)))
                .Subscriptions((p) => p.ForQueue<QueueMessage>(cfg => cfg.WithQueueName(QueueName)));
        }
    }

    private sealed class RegionContributor : IMessageBusConfigurationContributor
    {
        public void Configure(MessagingBusBuilder builder)
        {
            builder.Messaging((p) => p.WithRegion("eu-west-1"));
        }
    }

    private sealed class QueueMessage : Message
    { }

    private sealed class TopicMessage : Message
    { }


    private sealed class MyMonitor : IMessageMonitor
    {
        public void HandleException(Type messageType)
        { }

        public void HandleError(Exception ex, Amazon.SQS.Model.Message message)
        { }

        public void HandleThrottlingTime(TimeSpan duration)
        { }

        public void HandleTime(TimeSpan duration)
        { }

        public void Handled(Message message)
        { }

        public void IncrementThrottlingStatistic()
        { }

        public void IssuePublishingMessage()
        { }

        public void PublishMessageTime(TimeSpan duration)
        { }

        public void ReceiveMessageTime(TimeSpan duration, string queueName, string region)
        { }

        public void HandlerExecutionTime(Type handlerType, Type messageType, TimeSpan duration)
        { }
    }
}
