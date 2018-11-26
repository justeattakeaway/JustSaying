using System;
using System.Threading;
using System.Threading.Tasks;
using JustSaying.Fluent;
using JustSaying.Messaging.MessageHandling;
using JustSaying.Messaging.Monitoring;
using JustSaying.Models;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
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
        public void Can_Create_Messaging_Bus_Fluently()
        {
            // Arrange
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
                                    (options) => options.WithQueue<MyMessage>()
                                                        .WithTopic<MyMessage>())
                               .Subscriptions(
                                    (options) => options.WithSubscription<MyMessage>((p) => p.IntoQueue("foo")))
                               .Services(
                                    (options) => options.WithMessageMonitoring(() => new MyMonitor()));
                    })
                .AddJustSayingHandler<MyMessage, MyHandler>();

            IServiceProvider serviceProvider = services.BuildServiceProvider();

            // Assert
            var bus = serviceProvider.GetRequiredService<IMessagingBus>();
            bus.Start(new CancellationToken(canceled: true));
        }

        [Fact]
        public void Can_Create_Messaging_Bus()
        {
            // Arrange
            var services = new ServiceCollection()
                .AddLogging((p) => p.AddXUnit(OutputHelper))
                .AddJustSaying("eu-west-1")
                .AddJustSayingHandler<MyMessage, MyHandler>();

            IServiceProvider serviceProvider = services.BuildServiceProvider();

            // Assert
            var bus = serviceProvider.GetRequiredService<IMessagingBus>();
            bus.Start(new CancellationToken(canceled: true));
        }

        private sealed class MyMessage : Message
        {
        }

        private sealed class MyHandler : IHandlerAsync<MyMessage>
        {
            public Task<bool> Handle(MyMessage message)
            {
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
