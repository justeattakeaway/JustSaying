using System;
using System.Threading;
using System.Threading.Tasks;
using JustSaying.Fluent;
using JustSaying.Messaging;
using JustSaying.Messaging.MessageHandling;
using JustSaying.Messaging.Monitoring;
using JustSaying.Models;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Shouldly;
using Xunit;
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
        public async Task Can_Create_Messaging_Bus_Fluently()
        {
            // Arrange
            var handler2 = new Handler2();

            var services = new ServiceCollection()
                .AddLogging((p) => p.AddXUnit(OutputHelper))
                .AddJustSaying(
                    (builder) =>
                    {
                        builder.Client(
                                    (options) => options.WithBasicCredentials("accessKey", "secretKey")
                                                        .WithServiceUrl("http://localhost:4100"))
                               .Messaging(
                                    (options) => options.WithRegions("eu-west-1", "eu-central-1")
                                                        .WithActiveRegion("eu-west-1"))
                               .Publications(
                                    (options) => options.WithQueue<Message1>()
                                                        .WithTopic<Message2>())
                               .Subscriptions(
                                    (options) => options.ForQueue<Message1>((p) => p.WithName("foo"))
                                                        .ForTopic<Message2>((p) => p.WithName("bar")))
                               .Services(
                                    (options) => options.WithMessageMonitoring(() => new MyMonitor()));
                    })
                .AddJustSayingHandler<Message1, Handler1>()
                .AddSingleton<IHandlerAsync<Message2>>(handler2);

            IServiceProvider serviceProvider = services.BuildServiceProvider();
            IMessagingBus bus = serviceProvider.GetRequiredService<IMessagingBus>();

            using (var source = new CancellationTokenSource(TimeSpan.FromSeconds(30)))
            {
                // Act
                bus.Start(source.Token);

                IMessagePublisher publisher = bus as IMessagePublisher; // HACK For now before first-class support
                publisher.ShouldNotBeNull();

                var message1 = new Message1();
                var message2 = new Message2();

                await publisher.PublishAsync(message1);
                await publisher.PublishAsync(message2);

                // Assert
                while (!source.IsCancellationRequested && handler2.Count == 0)
                {
                    await Task.Delay(TimeSpan.FromSeconds(1), source.Token);
                }

                handler2.Count.ShouldBeGreaterThanOrEqualTo(1);
                handler2.LastId.ShouldBe(message2.Id);
            }
        }

        [Fact]
        public void Can_Create_Messaging_Bus()
        {
            // Arrange
            var services = new ServiceCollection()
                .AddLogging((p) => p.AddXUnit(OutputHelper))
                .AddJustSaying("eu-west-1")
                .AddJustSayingHandler<Message1, Handler1>();

            IServiceProvider serviceProvider = services.BuildServiceProvider();

            // Assert
            var bus = serviceProvider.GetRequiredService<IMessagingBus>();
            bus.Start(new CancellationToken(canceled: true));
        }

        private sealed class Message1 : Message
        {
        }

        private sealed class Message2 : Message
        {
        }

        private sealed class Handler1 : IHandlerAsync<Message1>
        {
            public Task<bool> Handle(Message1 message)
            {
                return Task.FromResult(true);
            }
        }

        private sealed class Handler2 : IHandlerAsync<Message2>
        {
            internal int Count { get; set; }

            internal Guid LastId { get; set; }

            public Task<bool> Handle(Message2 message)
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
