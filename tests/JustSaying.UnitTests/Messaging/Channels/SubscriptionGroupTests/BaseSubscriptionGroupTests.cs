using Amazon.SQS.Model;
using JustSaying.AwsTools.MessageHandling.Dispatch;
using JustSaying.Messaging;
using JustSaying.Messaging.Channels.Receive;
using JustSaying.Messaging.Channels.SubscriptionGroups;
using JustSaying.Messaging.Compression;
using JustSaying.Messaging.MessageHandling;
using JustSaying.Messaging.Middleware;
using JustSaying.TestingFramework;
using JustSaying.UnitTests.Messaging.Channels.Fakes;
using JustSaying.UnitTests.Messaging.Channels.TestHelpers;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using HandleMessageMiddleware = JustSaying.Messaging.Middleware.MiddlewareBase<JustSaying.Messaging.Middleware.HandleMessageContext, bool>;

namespace JustSaying.UnitTests.Messaging.Channels.SubscriptionGroupTests;

public abstract class BaseSubscriptionGroupTests : IAsyncLifetime
{
    protected IList<SqsSource> Queues;
    protected MiddlewareMap MiddlewareMap;
    protected TrackingLoggingMonitor Monitor;
    protected int ConcurrencyLimit = 8;
    protected ITestOutputHelper OutputHelper { get; }

    protected HandleMessageMiddleware Middleware;
    protected InspectableHandler<SimpleMessage> Handler;
    protected ISubscriptionGroup SystemUnderTest { get; private set; }
    protected ILoggerFactory LoggerFactory { get; }
    protected ILogger Logger { get; }
    protected CancellationTokenSource CancellationTokenSource { get; } = new();

    private readonly IMessageReceivePauseSignal _messageReceivePauseSignal;

    public BaseSubscriptionGroupTests(ITestOutputHelper testOutputHelper)
    {
        _messageReceivePauseSignal = new MessageReceivePauseSignal();
        OutputHelper = testOutputHelper;
        LoggerFactory = testOutputHelper.ToLoggerFactory();
        Logger = LoggerFactory.CreateLogger(GetType());
    }

    public async ValueTask InitializeAsync()
    {
        GivenInternal();

        SystemUnderTest = CreateSystemUnderTest();

        await WhenAsync().ConfigureAwait(false);
    }

    private void GivenInternal()
    {
        Queues = [];
        Handler = new InspectableHandler<SimpleMessage>();
        Monitor = new TrackingLoggingMonitor(LoggerFactory.CreateLogger<TrackingLoggingMonitor>());
        MiddlewareMap = new MiddlewareMap();
        CompletionMiddleware = new AwaitableMiddleware(OutputHelper, MessagesToWaitFor);
        SetupMessage = new SimpleMessage
        {
            RaisingComponent = "Component",
            Id = Guid.NewGuid()
        };

        var testResolver = new InMemoryServiceResolver(OutputHelper, Monitor,
            sc => sc.AddSingleton<IHandlerAsync<SimpleMessage>>(Handler));

        Middleware = new HandlerMiddlewareBuilder(testResolver, testResolver)
            .Use(CompletionMiddleware)
            .UseDefaults<SimpleMessage>(typeof(InspectableHandler<SimpleMessage>))
            .Build();

        Given();
    }

    public SimpleMessage SetupMessage { get; private set; }
    public int MessagesToWaitFor { get; protected set; } = 1;
    public AwaitableMiddleware CompletionMiddleware { get; set; }

    protected abstract void Given();

    // Default implementation
    protected virtual async Task WhenAsync()
    {
        foreach (SqsSource queue in Queues)
        {
            MiddlewareMap.Add<SimpleMessage>(queue.SqsQueue.QueueName, Middleware);
        }

        var cts = new CancellationTokenSource(TimeSpan.FromSeconds(20));
        using var linkedCts = CancellationTokenSource.CreateLinkedTokenSource(cts.Token, CancellationTokenSource.Token);
        var completion = SystemUnderTest.RunAsync(linkedCts.Token);

        await Patiently.AssertThatAsync(OutputHelper,
            async () => cts.IsCancellationRequested || await UntilAsync());

        await cts.CancelAsync();
        await completion.HandleCancellation();
    }

    protected virtual async Task<bool> UntilAsync()
    {
        OutputHelper.WriteLine("Checking if middleware chain has completed");
        await (CompletionMiddleware.Complete ?? Task.CompletedTask).WaitAsync(TimeSpan.FromSeconds(5)).ConfigureAwait(ConfigureAwaitOptions.SuppressThrowing);
        return CompletionMiddleware.Complete is not null;
    }

    private ISubscriptionGroup CreateSystemUnderTest()
    {
        Logger.LogInformation("Creating MessageDispatcher");

        var dispatcher = new MessageDispatcher(
            Monitor,
            MiddlewareMap,
            LoggerFactory);

        var defaults = new SubscriptionGroupSettingsBuilder()
            .WithDefaultConcurrencyLimit(ConcurrencyLimit);

        var subscriptionGroupFactory = new SubscriptionGroupFactory(
            dispatcher,
            _messageReceivePauseSignal,
            Monitor,
            LoggerFactory);

        var settings = SetupBusConfig();

        return subscriptionGroupFactory.Create(defaults, settings);
    }

    protected virtual Dictionary<string, SubscriptionGroupConfigBuilder> SetupBusConfig()
    {
        return new Dictionary<string, SubscriptionGroupConfigBuilder>
        {
            ["test"] = new SubscriptionGroupConfigBuilder("test").AddQueues(Queues)
        };
    }

    protected SqsSource CreateSuccessfulTestQueue(string queueName, params Message[] messages)
    {
        return CreateSuccessfulTestQueue(queueName, messages.AsEnumerable());
    }

    protected SqsSource CreateSuccessfulTestQueue(string queueName, IEnumerable<Message> messages)
    {
        return CreateSuccessfulTestQueue(queueName, ct => Task.FromResult(messages));
    }

    protected SqsSource CreateSuccessfulTestQueue(
        string queueName,
        Func<CancellationToken, Task<IEnumerable<Message>>> messageProducer)
    {
        var sqsQueue = new FakeSqsQueue(messageProducer, queueName);

        return new SqsSource
        {
            SqsQueue = sqsQueue,
            MessageConverter = new InboundMessageConverter(new FakeBodyDeserializer(
                    SetupMessage),
                new MessageCompressionRegistry(), false)
        };
    }

    public ValueTask DisposeAsync()
    {
        LoggerFactory?.Dispose();

        return ValueTask.CompletedTask;
    }

    protected class TestMessage : Message
    { }
}
