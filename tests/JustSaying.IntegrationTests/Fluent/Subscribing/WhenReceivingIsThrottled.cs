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
        // First handler takes ages all the others take 50 ms
        int waitOthers = 50;
        int waitOne = 3_600_000;

        var messagesToSend = Enumerable.Range(0, 30)
            .Select(i => new WaitingMessage(i, TimeSpan.FromMilliseconds(i == 0 ? waitOne : waitOthers)))
            .ToList();

        var handler = new InspectableWaitingHandler(OutputHelper);

        // Arrange
        var services = GivenJustSaying()
            .ConfigureJustSaying((builder) => builder.Client((client) => client.WithAnonymousCredentials()))
            .ConfigureJustSaying((builder) => builder.Messaging((options) => options.WithPublishFailureBackoff(TimeSpan.FromMilliseconds(1))))
            .ConfigureJustSaying((builder) => builder.Publications((options) => options.WithQueue<WaitingMessage>(o => o.WithName(UniqueName))))
            .ConfigureJustSaying(
                (builder) => builder.Subscriptions(
                    (options) => options
                        .WithSubscriptionGroup("group", groupConfig =>
                            groupConfig.WithConcurrencyLimit(2))
                        .ForQueue<WaitingMessage>((queue) => queue.WithQueueName(UniqueName)
                            .WithReadConfiguration(c =>
                                c.WithSubscriptionGroup("group")))))
            .AddSingleton<IHandlerAsync<WaitingMessage>>(handler);

        var baseSleep = TimeSpan.FromMilliseconds(500);

        await WhenAsync(
            services,
            async (publisher, listener, cancellationToken) =>
            {
                await listener.StartAsync(cancellationToken);
                await publisher.StartAsync(cancellationToken);

                // Publish the message with a long running handler
                await publisher.PublishAsync(messagesToSend.First(), cancellationToken);

                // Give some time to AWS to schedule the first long running message
                await Task.Delay(baseSleep, cancellationToken);

                foreach (var msg in messagesToSend.Skip(1).SkipLast(1))
                {
                    await publisher.PublishAsync(msg, cancellationToken);
                }

                // Publish the last message after a couple of seconds to guarantee it was scheduled after all the rest
                await Task.Delay(baseSleep, cancellationToken);
                await publisher.PublishAsync(messagesToSend.Last(), cancellationToken);

                // Wait for a reasonble time before asserting whether the last message has been scheduled.
                await Task.Delay(baseSleep * 2, cancellationToken);

                handler.ReceivedMessages.Count.ShouldBeGreaterThan(10);
                handler.ReceivedMessages.ShouldBeInOrder(SortDirection.Ascending);
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
        public override async Task<bool> Handle(WaitingMessage message)
        {
            await base.Handle(message);
            outputHelper.WriteLine($"Running task {message.Order} which will wait for {message.TimeToWait.TotalMilliseconds}ms");
            await Task.Delay(message.TimeToWait);
            return true;
        }
    }
}
