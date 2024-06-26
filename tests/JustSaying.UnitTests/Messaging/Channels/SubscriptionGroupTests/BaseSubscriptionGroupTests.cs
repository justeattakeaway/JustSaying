using Amazon.SQS.Model;
using JustSaying.AwsTools.MessageHandling;
using JustSaying.AwsTools.MessageHandling.Compression;
using JustSaying.AwsTools.MessageHandling.Dispatch;
using JustSaying.Messaging.Channels.Receive;
using JustSaying.Messaging.Channels.SubscriptionGroups;
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
    protected IList<ISqsQueue> Queues;
    protected MiddlewareMap MiddlewareMap;
    protected TrackingLoggingMonitor Monitor;
    protected FakeSerializationRegister SerializationRegister;
    protected int ConcurrencyLimit = 8;
    protected ITestOutputHelper OutputHelper { get; }

    protected HandleMessageMiddleware Middleware;
    protected InspectableHandler<SimpleMessage> Handler;
    protected ISubscriptionGroup SystemUnderTest { get; private set; }
    protected ILoggerFactory LoggerFactory { get; }
    protected ILogger Logger { get; }

    private readonly IMessageReceivePauseSignal _messageReceivePauseSignal;

    public BaseSubscriptionGroupTests(ITestOutputHelper testOutputHelper)
    {
        _messageReceivePauseSignal = new MessageReceivePauseSignal();
        OutputHelper = testOutputHelper;
        LoggerFactory = testOutputHelper.ToLoggerFactory();
        Logger = LoggerFactory.CreateLogger(GetType());
    }

    public async Task InitializeAsync()
    {
        GivenInternal();

        SystemUnderTest = CreateSystemUnderTest();

        await WhenAsync().ConfigureAwait(false);
    }

    private void GivenInternal()
    {
        Queues = new List<ISqsQueue>();
        Handler = new InspectableHandler<SimpleMessage>();
        Monitor = new TrackingLoggingMonitor(LoggerFactory.CreateLogger<TrackingLoggingMonitor>());
        SerializationRegister = new FakeSerializationRegister();
        MiddlewareMap = new MiddlewareMap();
        CompletionMiddleware = new AwaitableMiddleware(OutputHelper);

        var testResolver = new InMemoryServiceResolver(OutputHelper, Monitor,
            sc => sc.AddSingleton<IHandlerAsync<SimpleMessage>>(Handler));

        Middleware = new HandlerMiddlewareBuilder(testResolver, testResolver)
            .Use(CompletionMiddleware)
            .UseDefaults<SimpleMessage>(typeof(InspectableHandler<SimpleMessage>))
            .Build();

        Given();
    }

    public AwaitableMiddleware CompletionMiddleware { get; set; }

    protected abstract void Given();

    // Default implementation
    protected virtual async Task WhenAsync()
    {
        foreach (ISqsQueue queue in Queues)
        {
            MiddlewareMap.Add<SimpleMessage>(queue.QueueName, Middleware);
        }

        var cts = new CancellationTokenSource(TimeSpan.FromSeconds(20));
        var completion = SystemUnderTest.RunAsync(cts.Token);

        await Patiently.AssertThatAsync(OutputHelper,
            () => Until() || cts.IsCancellationRequested);

        cts.Cancel();
        await completion.HandleCancellation();
    }

    protected virtual bool Until()
    {
        OutputHelper.WriteLine("Checking if middleware chain has completed");
        return CompletionMiddleware.Complete?.IsCompleted ?? false;
    }

    private ISubscriptionGroup CreateSystemUnderTest()
    {
        Logger.LogInformation("Creating MessageDispatcher with serialization register type {Type}",
            SerializationRegister.GetType().FullName);

        var dispatcher = new MessageDispatcher(
            SerializationRegister,
            Monitor,
            MiddlewareMap,
            new MessageCompressionRegistry([]),
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
            { "test", new SubscriptionGroupConfigBuilder("test").AddQueues(Queues) },
        };
    }

    protected static FakeSqsQueue CreateSuccessfulTestQueue(string queueName, params Message[] messages)
    {
        return CreateSuccessfulTestQueue(queueName, messages.AsEnumerable());
    }

    protected static FakeSqsQueue CreateSuccessfulTestQueue(string queueName, IEnumerable<Message> messages)
    {
        return CreateSuccessfulTestQueue(queueName, ct => Task.FromResult(messages));
    }

    protected static FakeSqsQueue CreateSuccessfulTestQueue(
        string queueName,
        Func<CancellationToken, Task<IEnumerable<Message>>> messageProducer)
    {
        var sqsQueue = new FakeSqsQueue( messageProducer,
            queueName);

        return sqsQueue;
    }

    public Task DisposeAsync()
    {
        LoggerFactory?.Dispose();

        return Task.CompletedTask;
    }

    protected class TestMessage : Message
    { }
}
