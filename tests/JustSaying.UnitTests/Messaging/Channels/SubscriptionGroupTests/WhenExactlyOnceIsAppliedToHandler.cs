using JustSaying.AwsTools.MessageHandling;
using JustSaying.Messaging.MessageHandling;
using JustSaying.Messaging.Middleware;
using JustSaying.TestingFramework;
using JustSaying.UnitTests.Messaging.Channels.Fakes;
using JustSaying.UnitTests.Messaging.Channels.TestHelpers;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;

namespace JustSaying.UnitTests.Messaging.Channels.SubscriptionGroupTests;

public class WhenExactlyOnceIsAppliedToHandler(ITestOutputHelper testOutputHelper) : BaseSubscriptionGroupTests(testOutputHelper)
{
    private ISqsQueue _queue;
    private readonly int _expectedTimeout = 5;
    private FakeMessageLock _messageLock;

    protected override void Given()
    {
        _queue = CreateSuccessfulTestQueue("TestQueue", new TestMessage());

        Queues.Add(_queue);

        _messageLock = new FakeMessageLock();

        var serviceResolver = new InMemoryServiceResolver(sc =>
            sc.AddSingleton<IMessageLockAsync>(_messageLock)
                .AddSingleton<IHandlerAsync<SimpleMessage>>(Handler)
                .AddLogging(x => x.AddXUnit(OutputHelper)));

        var middlewareBuilder = new HandlerMiddlewareBuilder(serviceResolver, serviceResolver);

        var middleware = middlewareBuilder.Configure(pipe =>
        {
            pipe.UseExactlyOnce<SimpleMessage>("a-unique-lock-key", TimeSpan.FromSeconds(5));
            pipe.UseHandler<SimpleMessage>();
        }).Build();

        Middleware = middleware;
    }

    protected override async Task WhenAsync()
    {
        MiddlewareMap.Add<SimpleMessage>(_queue.QueueName, Middleware);

        using var cts = new CancellationTokenSource();

        var completion = SystemUnderTest.RunAsync(cts.Token);

        // wait until it's done
        await Patiently.AssertThatAsync(OutputHelper,
            () => Handler.ReceivedMessages.Any());

        cts.Cancel();

        await Assert.ThrowsAnyAsync<OperationCanceledException>(() => completion);
    }

    [Fact]
    public void ProcessingIsPassedToTheHandler()
    {
        Handler.ReceivedMessages.ShouldNotBeEmpty();
    }

    [Fact]
    public void MessageIsLocked()
    {
        var messageId = SerializationRegister.DefaultDeserializedMessage().Id.ToString();

        var tempLockRequests = _messageLock.MessageLockRequests.Where(lr => !lr.isPermanent);
        tempLockRequests.Count().ShouldBeGreaterThan(0);
        tempLockRequests.ShouldAllBe(pair =>
            pair.key.Contains(messageId, StringComparison.OrdinalIgnoreCase) &&
            pair.howLong == TimeSpan.FromSeconds(_expectedTimeout));
    }
}