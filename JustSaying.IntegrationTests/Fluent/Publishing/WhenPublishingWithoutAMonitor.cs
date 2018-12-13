using System;
using System.Threading;
using System.Threading.Tasks;
using JustSaying.Messaging;
using JustSaying.Messaging.MessageHandling;
using JustSaying.TestingFramework;
using Microsoft.Extensions.DependencyInjection;
using NSubstitute;
using Xunit.Abstractions;

namespace JustSaying.IntegrationTests.Fluent.Publishing
{
    public class WhenPublishingWithoutAMonitor : IntegrationTestBase
    {
        public WhenPublishingWithoutAMonitor(ITestOutputHelper outputHelper)
            : base(outputHelper)
        {
        }

        [AwsFact]
        public async Task A_Message_Can_Still_Be_Published_To_A_Queue()
        {
            // Arrange
            var completion = new TaskCompletionSource<object>();

            IHandlerAsync<SimpleMessage> handler = CreateHandler<SimpleMessage>(completion);

            IServiceProvider serviceProvider = Given(
                (builder) =>
                {
                    builder.Publications((p) => p.WithQueue<SimpleMessage>(UniqueName))
                           .Subscriptions((p) => p.ForQueue<SimpleMessage>(UniqueName));
                })
                .AddSingleton(handler)
                .BuildServiceProvider();

            IMessagePublisher publisher = serviceProvider.GetRequiredService<IMessagePublisher>();
            IMessagingBus listener = serviceProvider.GetRequiredService<IMessagingBus>();

            using (var source = new CancellationTokenSource(TimeSpan.FromSeconds(20)))
            {
                listener.Start(source.Token);

                var message = new SimpleMessage();

                // Act
                await publisher.PublishAsync(message, source.Token);

                // Assert
                completion.Task.Wait(source.Token);

                await handler.Received(1).Handle(Arg.Any<SimpleMessage>());
            }
        }

        [AwsFact]
        public async Task A_Message_Can_Still_Be_Published_To_A_Topic()
        {
            // Arrange
            var completion = new TaskCompletionSource<object>();

            IHandlerAsync<SimpleMessage> handler = CreateHandler<SimpleMessage>(completion);

            IServiceProvider serviceProvider = Given(
                (builder) =>
                {
                    builder.Publications((publication) => publication.WithTopic<SimpleMessage>());

                    builder.Messaging(
                        (config) => config.WithPublishFailureBackoff(TimeSpan.FromMilliseconds(1))
                                          .WithPublishFailureReattempts(1));

                    builder.Subscriptions(
                        (subscription) => subscription.ForTopic<SimpleMessage>(
                            (topic) => topic.WithName(UniqueName).WithReadConfiguration(
                                (config) => config.WithInstancePosition(1))));
                })
                .AddSingleton(handler)
                .BuildServiceProvider();

            IMessagePublisher publisher = serviceProvider.GetRequiredService<IMessagePublisher>();
            IMessagingBus listener = serviceProvider.GetRequiredService<IMessagingBus>();

            using (var source = new CancellationTokenSource(TimeSpan.FromSeconds(20)))
            {
                listener.Start(source.Token);

                var message = new SimpleMessage();

                // Act
                await publisher.PublishAsync(message, source.Token);

                // Assert
                completion.Task.Wait(source.Token);

                await handler.Received(1).Handle(Arg.Any<SimpleMessage>());
            }
        }
    }
}
