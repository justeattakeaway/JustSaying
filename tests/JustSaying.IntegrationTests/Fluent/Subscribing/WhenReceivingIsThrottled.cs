using System.Collections.Concurrent;
using JustSaying.Messaging.MessageHandling;
using JustSaying.Models;
using JustSaying.TestingFramework;
using Microsoft.Extensions.DependencyInjection;

namespace JustSaying.IntegrationTests.Fluent.Subscribing;

/// <summary>
/// This test ensures that when one messages takes a long time, other messages will still be able to
/// be processed, and will not consume more concurrency than is available. That means that with a maximum
/// concurrency of 2, the long running handler will consume one slot, and every other message will be
/// processed in order in the other slot. The result should be exactly in-order delivery for this test.
/// </summary>
public sealed class WhenReceivingIsThrottled(ITestOutputHelper outputHelper) : IntegrationTestBase(outputHelper)
{
    [AwsFact]
    public async Task Then_The_Messages_Are_Handled_With_Throttle()
    {
        // First handler takes ages all the others take 10 ms
        int waitOthers = 10;
        int waitOne = 3_600_000;

        var messagesToSend = Enumerable.Range(0, 30)
            .Select(i => new WaitingMessage(i, TimeSpan.FromMilliseconds(i == 0 ? waitOne : waitOthers)))
            .ToList();

        var handler = new InspectableWaitingHandler(OutputHelper);

        // Arrange
        var services = GivenJustSaying()
            .ConfigureJustSaying((builder) => builder.Client((client) => client.WithAnonymousCredentials()))
            .ConfigureJustSaying((builder) => builder.Messaging((options) => options.WithPublishFailureBackoff(TimeSpan.FromMilliseconds(1))))
            .ConfigureJustSaying((builder) => builder.Publications((options) => options.WithQueue<WaitingMessage>(o => o.WithQueueName(UniqueName))))
            .ConfigureJustSaying(
                (builder) => builder.Subscriptions(
                    (options) => options
                        .WithSubscriptionGroup("group", groupConfig =>
                            groupConfig.WithConcurrencyLimit(2))
                        .ForQueue<WaitingMessage>((queue) => queue.WithQueueName(UniqueName)
                            .WithReadConfiguration(c =>
                                c.WithSubscriptionGroup("group")))))
            .AddSingleton<IHandlerAsync<WaitingMessage>>(handler);

        await WhenAsync(
            services,
            async (publisher, listener, cancellationToken) =>
            {
                await listener.StartAsync(cancellationToken);
                await publisher.StartAsync(cancellationToken);

                // Publish the message with a long-running handler
                await publisher.PublishAsync(messagesToSend.First(), cancellationToken);

                // Wait until the first (long-running) message has started processing
                // This ensures it occupies one of the two concurrency slots
                await handler.WaitForFirstMessageStartedAsync(cancellationToken);

                // Now publish the remaining messages
                foreach (var msg in messagesToSend.Skip(1))
                {
                    await publisher.PublishAsync(msg, cancellationToken);
                }

                // Wait for all short messages (messages 1-29) to complete
                // Message 0 is still running and won't complete during this test
                await handler.WaitForCompletedCountAsync(29, cancellationToken);

                // Get the completed messages (excluding the still-running message 0)
                var completedMessages = handler.CompletedMessages.ToList();
                completedMessages.Count.ShouldBeGreaterThanOrEqualTo(29);

                // Verify that messages 1-29 were processed in order
                // (they should be, since only one slot was available after message 0 started)
                var shortMessages = completedMessages.Where(m => m.Order > 0).ToList();
                shortMessages.ShouldBeInOrder(SortDirection.Ascending);
            });
    }

    private class WaitingMessage(int order, TimeSpan timeToWait) : Message, IComparable<WaitingMessage>
    {
        public TimeSpan TimeToWait { get; } = timeToWait;
        public int Order { get; } = order;

        public int CompareTo(WaitingMessage other)
        {
            if (ReferenceEquals(this, other)) return 0;
            if (other is null) return 1;
            return Order.CompareTo(other.Order);
        }
    }

    private class InspectableWaitingHandler(ITestOutputHelper outputHelper) : InspectableHandler<WaitingMessage>
    {
        private readonly TaskCompletionSource _firstMessageStarted = new(TaskCreationOptions.RunContinuationsAsynchronously);
        private readonly ConcurrentQueue<WaitingMessage> _completedMessages = new();
        private int _completedCount;

        public ConcurrentQueue<WaitingMessage> CompletedMessages => _completedMessages;

        public Task WaitForFirstMessageStartedAsync(CancellationToken cancellationToken)
        {
            cancellationToken.Register(() => _firstMessageStarted.TrySetCanceled(cancellationToken));
            return _firstMessageStarted.Task;
        }

        public async Task WaitForCompletedCountAsync(int count, CancellationToken cancellationToken)
        {
            while (Volatile.Read(ref _completedCount) < count)
            {
                cancellationToken.ThrowIfCancellationRequested();
                await Task.Delay(10, cancellationToken);
            }
        }

        public override async Task<bool> Handle(WaitingMessage message)
        {
            await base.Handle(message);
            outputHelper.WriteLine($"Running task {message.Order} which will wait for {message.TimeToWait.TotalMilliseconds}ms");

            // Signal that the first message has started (this allows the test to proceed)
            _firstMessageStarted.TrySetResult();

            await Task.Delay(message.TimeToWait);

            _completedMessages.Enqueue(message);
            Interlocked.Increment(ref _completedCount);

            return true;
        }
    }
}
