using System;
using System.Threading;
using System.Threading.Tasks;
using JustSaying.Fluent;
using JustSaying.Messaging.MessageHandling;
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
                                                        .And()
                                                        .WithServiceUrl("http://localhost:4100"))
                               .Messaging(
                                    (options) => options.WithRegions("eu-west-1", "eu-central-1")
                                                        .And()
                                                        .WithActiveRegion("eu-west-1"))
                               .Subscriptions(
                                    (options) => options.WithHandler<MyMessage>());
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
    }
}
