using JustSaying.AwsTools;
using JustSaying.Fluent;
using JustSaying.Messaging.Channels.Receive;
using JustSaying.Messaging.MessageSerialization;
using JustSaying.Messaging.Middleware;
using JustSaying.Messaging.Monitoring;
using JustSaying.UnitTests.Messaging.Channels.Fakes;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using NSubstitute;

namespace JustSaying.UnitTests.Fluent;

public class PublicationsBuilderTests
{
    [Fact]
    public void WithPublishMiddleware_ConfigureSetsGlobalMiddlewareOnBus()
    {
        var loggerFactory = Substitute.For<ILoggerFactory>();
        loggerFactory.CreateLogger(Arg.Any<string>()).Returns(Substitute.For<ILogger>());
        var config = Substitute.For<IMessagingConfig>();
        var monitor = Substitute.For<IMessageMonitor>();

        var bus = new JustSaying.JustSayingBus(
            config,
            new NewtonsoftSerializationFactory(),
            new MessageReceivePauseSignal(),
            loggerFactory,
            monitor);

        var resolver = new InMemoryServiceResolver(sc =>
            sc.AddTransient<TestPublishMiddleware>());

        var messagingBusBuilder = new MessagingBusBuilder();
        var publicationsBuilder = new PublicationsBuilder(messagingBusBuilder);
        publicationsBuilder.WithPublishMiddleware<TestPublishMiddleware>();

        var proxy = Substitute.For<IAwsClientFactoryProxy>();

        publicationsBuilder.Configure(bus, proxy, loggerFactory, resolver);

        bus.PublishMiddleware.ShouldNotBeNull();
    }

    private sealed class TestPublishMiddleware : MiddlewareBase<PublishContext, bool>
    {
        protected override Task<bool> RunInnerAsync(
            PublishContext context,
            Func<CancellationToken, Task<bool>> func,
            CancellationToken stoppingToken)
            => func(stoppingToken);
    }
}
