using System;
using System.Threading;
using System.Threading.Tasks;
using JustSaying.Fluent;
using JustSaying.Messaging;
using JustSaying.Messaging.MessageHandling;
using JustSaying.Messaging.Monitoring;
using JustSaying.Models;
using JustSaying.TestingFramework;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Shouldly;
using Xunit.Abstractions;

namespace JustSaying.IntegrationTests
{
    public class MessagingBusBuilderTests
    {
        public MessagingBusBuilderTests(ITestOutputHelper outputHelper)
        {
            OutputHelper = outputHelper;
        }

        private ITestOutputHelper OutputHelper { get; }

        [AwsFact]
        public async Task Can_Create_Messaging_Bus_Fluently_For_A_Queue()
        {
            // Arrange
            var services = new ServiceCollection()
                .AddLogging((p) => p.AddXUnit(OutputHelper))
                .AddJustSaying(
                    (builder) =>
                    {
                        builder.Client((options) => options.WithBasicCredentials("accessKey", "secretKey").WithServiceUri(TestEnvironment.SimulatorUrl))
                               .Messaging((options) => options.WithRegions("eu-west-1"))
                               .Publications((options) => options.WithQueue<QueueMessage>())
                               .Subscriptions((options) => options.ForQueue<QueueMessage>())
                               .Services((options) => options.WithMessageMonitoring(() => new MyMonitor()));
                    })
                .AddJustSayingHandler<QueueMessage, QueueHandler>();

            IServiceProvider serviceProvider = services.BuildServiceProvider();

            IMessagePublisher publisher = serviceProvider.GetRequiredService<IMessagePublisher>();
            IMessagingBus listener = serviceProvider.GetRequiredService<IMessagingBus>();

            using (var source = new CancellationTokenSource(TimeSpan.FromSeconds(20)))
            {
                // Act
                listener.Start(source.Token);

                var message = new QueueMessage();

                await publisher.PublishAsync(message, source.Token);

                // Assert
                while (!source.IsCancellationRequested && QueueHandler.Count == 0)
                {
                    await Task.Delay(TimeSpan.FromSeconds(1), source.Token);
                }

                QueueHandler.Count.ShouldBeGreaterThanOrEqualTo(1);
                QueueHandler.LastId.ShouldBe(message.Id);
            }
        }

        [AwsFact]
        public async Task Can_Create_Messaging_Bus_Fluently_For_A_Topic()
        {
            // Arrange
            var services = new ServiceCollection()
                .AddLogging((p) => p.AddXUnit(OutputHelper))
                .AddJustSaying(
                    (builder) =>
                    {
                        builder.Client((options) => options.WithBasicCredentials("accessKey", "secretKey").WithServiceUri(TestEnvironment.SimulatorUrl))
                               .Messaging((options) => options.WithRegions("eu-west-1"))
                               .Publications((options) => options.WithTopic<TopicMessage>())
                               .Subscriptions((options) => options.ForTopic<TopicMessage>());
                    })
                .AddJustSayingHandler<TopicMessage, TopicHandler>();

            IServiceProvider serviceProvider = services.BuildServiceProvider();

            IMessagePublisher publisher = serviceProvider.GetRequiredService<IMessagePublisher>();
            IMessagingBus listener = serviceProvider.GetRequiredService<IMessagingBus>();

            using (var source = new CancellationTokenSource(TimeSpan.FromSeconds(20)))
            {
                // Act
                listener.Start(source.Token);

                var message = new TopicMessage();

                await publisher.PublishAsync(message, source.Token);

                // Assert
                while (!source.IsCancellationRequested && TopicHandler.Count == 0)
                {
                    await Task.Delay(TimeSpan.FromSeconds(1), source.Token);
                }

                TopicHandler.Count.ShouldBeGreaterThanOrEqualTo(1);
                TopicHandler.LastId.ShouldBe(message.Id);
            }
        }

        [AwsFact]
        public void Can_Create_Messaging_Bus()
        {
            // Arrange
            var services = new ServiceCollection()
                .AddLogging((p) => p.AddXUnit(OutputHelper))
                .AddJustSaying("eu-west-1")
                .AddJustSayingHandler<QueueMessage, QueueHandler>();

            IServiceProvider serviceProvider = services.BuildServiceProvider();

            IMessagePublisher publisher = serviceProvider.GetRequiredService<IMessagePublisher>();
            IMessagingBus listener = serviceProvider.GetRequiredService<IMessagingBus>();

            using (var source = new CancellationTokenSource(TimeSpan.FromSeconds(20)))
            {
                // Act
                listener.Start(source.Token);
            }
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
                .AddJustSayingHandler<QueueMessage, QueueHandler>()
                .AddSingleton<MyMonitor>();

            IServiceProvider serviceProvider = services.BuildServiceProvider();

            IMessagePublisher publisher = serviceProvider.GetRequiredService<IMessagePublisher>();
            IMessagingBus listener = serviceProvider.GetRequiredService<IMessagingBus>();

            using (var source = new CancellationTokenSource(TimeSpan.FromSeconds(20)))
            {
                // Act
                listener.Start(source.Token);

                var message = new QueueMessage();

                await publisher.PublishAsync(message, source.Token);

                // Assert
                while (!source.IsCancellationRequested && QueueHandler.Count == 0)
                {
                    await Task.Delay(TimeSpan.FromSeconds(1), source.Token);
                }

                QueueHandler.Count.ShouldBeGreaterThanOrEqualTo(1);
                QueueHandler.LastId.ShouldBe(message.Id);
            }
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

        private sealed class MessagingContributor : IMessageBusConfigurationContributor
        {
            public MessagingContributor(IServiceProvider serviceProvider)
            {
                ServiceProvider = serviceProvider;
            }

            private IServiceProvider ServiceProvider { get; }

            public void Configure(MessagingBusBuilder builder)
            {
                builder.Services((p) => p.WithMessageMonitoring(ServiceProvider.GetRequiredService<MyMonitor>));
            }
        }

        private sealed class QueueContributor : IMessageBusConfigurationContributor
        {
            public void Configure(MessagingBusBuilder builder)
            {
                builder.Publications((p) => p.WithQueue<QueueMessage>())
                       .Subscriptions((p) => p.ForQueue<QueueMessage>());
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
        {
        }

        private sealed class QueueHandler : IHandlerAsync<QueueMessage>
        {
            internal static int Count { get; set; }

            internal static Guid LastId { get; set; }

            public Task<bool> Handle(QueueMessage message)
            {
                Count++;
                LastId = message.Id;

                return Task.FromResult(true);
            }
        }

        private sealed class TopicMessage : Message
        {
        }

        private sealed class TopicHandler : IHandlerAsync<TopicMessage>
        {
            internal static int Count { get; set; }

            internal static Guid LastId { get; set; }

            public Task<bool> Handle(TopicMessage message)
            {
                Count++;
                LastId = message.Id;

                return Task.FromResult(true);
            }
        }

        private sealed class MyMonitor : IMessageMonitor
        {
            public void HandleException(Type messageType)
            {
            }

            public void HandleThrottlingTime(long handleTimeMs)
            {
            }

            public void HandleTime(long handleTimeMs)
            {
            }

            public void IncrementThrottlingStatistic()
            {
            }

            public void IssuePublishingMessage()
            {
            }

            public void PublishMessageTime(long handleTimeMs)
            {
            }

            public void ReceiveMessageTime(long handleTimeMs, string queueName, string region)
            {
            }
        }
    }
}
