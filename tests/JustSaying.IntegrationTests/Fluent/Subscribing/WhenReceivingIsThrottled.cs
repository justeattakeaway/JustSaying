using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using JustSaying.Messaging;
using JustSaying.Messaging.MessageHandling;
using JustSaying.TestingFramework;
using Microsoft.Extensions.DependencyInjection;
using NSubstitute;
using Xunit.Abstractions;

namespace JustSaying.IntegrationTests.Fluent.Subscribing
{
    public sealed class WhenReceivingIsThrottled : IntegrationTestBase
    {
        private readonly IHandlerAsync<SimpleMessage> _handler = Substitute.For<IHandlerAsync<SimpleMessage>>();
        private readonly Dictionary<int, Guid> _ids = new Dictionary<int, Guid>();
        private readonly Dictionary<int, SimpleMessage> _messages = new Dictionary<int, SimpleMessage>();

        public WhenReceivingIsThrottled(ITestOutputHelper outputHelper)
            : base(outputHelper)
        {
            // First handler takes ages all the others take 100 ms
            int waitOthers = 100;
            int waitOne = TestEnvironment.IsSimulatorConfigured ? waitOthers : 3_600_000;

            for (int i = 1; i <= 100; i++)
            {
                _ids.Add(i, Guid.NewGuid());
                _messages.Add(i, new SimpleMessage() { Id = _ids[i] });

                SetUpHandler(_ids[i], i, waitMilliseconds: i == 1 ? waitOne : waitOthers);
            }
        }

        [AwsFact]
        public async Task Then_The_Messages_Are_Handled_With_Throttle()
        {
            // Arrange
            var services = GivenJustSaying()
                .ConfigureJustSaying((builder) => builder.Client((client) => client.WithAnonymousCredentials()))
                .ConfigureJustSaying((builder) => builder.Messaging((options) => options.WithPublishFailureBackoff(TimeSpan.FromMilliseconds(1))))
                .ConfigureJustSaying((builder) => builder.Publications((options) => options.WithQueue<SimpleMessage>(UniqueName)))
                .ConfigureJustSaying(
                    (builder) => builder.Subscriptions(
                        (options) => options
                            .WithSubscriptionGroup("group", groupConfig =>
                                groupConfig.WithConcurrencyLimit(10))
                            .ForQueue<SimpleMessage>((queue) => queue.WithName(UniqueName)
                                .WithReadConfiguration(c =>
                                    c.WithSubscriptionGroup("group")))))
                .AddSingleton(_handler);

            var baseSleep = TimeSpan.FromSeconds(2);

            await WhenAsync(
                services,
                async (publisher, listener, cancellationToken) =>
                {
                    _ = listener.StartAsync(cancellationToken);

                    // Publish the message with a long running handler
                    await publisher.PublishAsync(_messages[1], cancellationToken);

                    // Give some time to AWS to schedule the first long running message
                    await Task.Delay(baseSleep, cancellationToken);

                    // Publish the rest of the messages except the last one.
                    for (int i = 2; i <= 98; i++)
                    {
                        await publisher.PublishAsync(_messages[i], cancellationToken);
                    }

                    // Publish the last message after a couple of seconds to guarantee it was scheduled after all the rest
                    await Task.Delay(baseSleep, cancellationToken);
                    await publisher.PublishAsync(_messages[100], cancellationToken);

                    // Wait for a reasonble time before asserting whether the last message has been scheduled.
                    await Task.Delay(baseSleep, cancellationToken);

                    Received.InOrder(() => _handler.Handle(Arg.Is<SimpleMessage>((p) => p.Id == _ids[100])));
                });
        }

        private void SetUpHandler(Guid id, int number, int waitMilliseconds)
        {
            _handler
                .When((handler) => handler.Handle(Arg.Is<SimpleMessage>(y => y.Id == id)))
                .Do(
                    (_) =>
                    {
                        OutputHelper.WriteLine($"Running task {number}.");
                        Thread.Sleep(waitMilliseconds);
                    });
        }
    }
}
