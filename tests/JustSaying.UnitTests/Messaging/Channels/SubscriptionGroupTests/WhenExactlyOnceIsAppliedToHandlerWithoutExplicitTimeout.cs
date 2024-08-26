using JustSaying.AwsTools.MessageHandling;
using JustSaying.Messaging.Channels.SubscriptionGroups;
using JustSaying.Messaging.MessageHandling;
using JustSaying.Messaging.Middleware;
using JustSaying.TestingFramework;
using JustSaying.UnitTests.Messaging.Channels.Fakes;
using JustSaying.UnitTests.Messaging.Channels.TestHelpers;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;

namespace JustSaying.UnitTests.Messaging.Channels.SubscriptionGroupTests;

public class WhenExactlyOnceIsAppliedWithoutSpecificTimeout(ITestOutputHelper testOutputHelper) : BaseSubscriptionGroupTests(testOutputHelper)
{
    private SqsSource _queue;
    private readonly int _maximumTimeout = (int)TimeSpan.MaxValue.TotalSeconds;
    private FakeMessageLock _messageLock;

    protected override void Given()
    {
        _queue = CreateSuccessfulTestQueue(Guid.NewGuid().ToString(), new TestMessage());
        Queues.Add(_queue);
        _messageLock = new FakeMessageLock();

        var serviceResolver = new InMemoryServiceResolver(sc =>
            sc.AddSingleton<IMessageLockAsync>(_messageLock)
                .AddSingleton<IHandlerAsync<SimpleMessage>>(Handler)
                .AddLogging(x => x.AddXUnit(OutputHelper)));

        var middlewareBuilder = new HandlerMiddlewareBuilder(serviceResolver, serviceResolver);

        var middleware = middlewareBuilder.Configure(pipe =>
        {
            pipe.UseExactlyOnce<SimpleMessage>("a-unique-lock-key");
            pipe.UseHandler<SimpleMessage>();
        }).Build();

        Middleware = middleware;
    }

    protected override async Task WhenAsync()
    {
        MiddlewareMap.Add<SimpleMessage>(_queue.SqsQueue.QueueName, Middleware);

        var cts = new CancellationTokenSource();

        var completion = SystemUnderTest.RunAsync(cts.Token);

        await Patiently.AssertThatAsync(OutputHelper,
            () => Handler.ReceivedMessages.Any());

        cts.Cancel();
        await Assert.ThrowsAnyAsync<OperationCanceledException>(() => completion);

    }

    [Fact]
    public void MessageIsLocked()
    {
        // this should be part of setup to make work
        var message = new SimpleMessage
        {
            RaisingComponent = "Component",
            Id = Guid.NewGuid()
        };
        var messageId = message.Id.ToString();

        var tempLockRequests = _messageLock.MessageLockRequests.Where(lr => !lr.isPermanent);
        tempLockRequests.ShouldNotBeEmpty();

        foreach(var lockRequest in tempLockRequests)
        {
            lockRequest.key.ShouldContain(messageId, Case.Insensitive);
            ((int)lockRequest.howLong.TotalSeconds).ShouldBe(_maximumTimeout);
        }
    }
}
