using JustSaying.AwsTools.MessageHandling;
using JustSaying.Messaging;
using JustSaying.Messaging.Channels.Receive;
using JustSaying.Messaging.MessageSerialization;
using JustSaying.Models;
using JustSaying.TestingFramework;
using JustSaying.UnitTests.Messaging.Channels.TestHelpers;
using Microsoft.Extensions.Logging;
using NSubstitute;
using NSubstitute.ExceptionExtensions;

namespace JustSaying.UnitTests.JustSayingBus;

/// <summary>
/// Publish re-attempts exist for transient AWS failures. A message that can't be serialized fails the
/// same way on every attempt, and a publish the caller cancelled shouldn't be attempted again, so both
/// fail fast with the original exception.
/// </summary>
public class WhenPublishingFailsDeterministically
{
    private const int PublishAttempts = 3;

    [Test]
    [Arguments(typeof(System.Text.Json.JsonException))]
    [Arguments(typeof(Newtonsoft.Json.JsonSerializationException))]
    [Arguments(typeof(NotSupportedException))]
    [Arguments(typeof(ArgumentException))]
    public async Task ASerializationFailureIsNotRetried(Type exceptionType)
    {
        var exception = (Exception)Activator.CreateInstance(exceptionType);
        var publisher = Substitute.For<IMessagePublisher>();
        publisher.PublishAsync(Arg.Any<object>(), Arg.Any<PublishMetadata>(), Arg.Any<CancellationToken>())
            .ThrowsAsync(exception);

        var bus = await CreateStartedBusAsync(publisher, null);

        var thrown = await Should.ThrowAsync<Exception>(() => bus.PublishAsync(new SimpleMessage(), CancellationToken.None));

        thrown.ShouldBeSameAs(exception);
        await publisher.Received(1).PublishAsync(Arg.Any<object>(), Arg.Any<PublishMetadata>(), Arg.Any<CancellationToken>());
    }

    [Test]
    public async Task ATransientFailureIsStillRetried()
    {
        var publisher = Substitute.For<IMessagePublisher>();
        publisher.PublishAsync(Arg.Any<object>(), Arg.Any<PublishMetadata>(), Arg.Any<CancellationToken>())
            .ThrowsAsync(new PublishException("Thrown by test"));

        var bus = await CreateStartedBusAsync(publisher, null);

        await Should.ThrowAsync<PublishException>(() => bus.PublishAsync(new SimpleMessage(), CancellationToken.None));

        await publisher.Received(PublishAttempts).PublishAsync(Arg.Any<object>(), Arg.Any<PublishMetadata>(), Arg.Any<CancellationToken>());
    }

    [Test]
    public async Task ACancelledPublishIsNotRetried()
    {
        using var cts = new CancellationTokenSource();
        var publisher = Substitute.For<IMessagePublisher>();
        publisher.PublishAsync(Arg.Any<object>(), Arg.Any<PublishMetadata>(), Arg.Any<CancellationToken>())
            .Returns(_ =>
            {
                cts.Cancel();
                return Task.FromException(new OperationCanceledException(cts.Token));
            });

        var bus = await CreateStartedBusAsync(publisher, null);

        await Should.ThrowAsync<OperationCanceledException>(() => bus.PublishAsync(new SimpleMessage(), cts.Token));

        await publisher.Received(1).PublishAsync(Arg.Any<object>(), Arg.Any<PublishMetadata>(), Arg.Any<CancellationToken>());
    }

    [Test]
    public async Task ABatchSerializationFailureIsNotRetried()
    {
        var exception = new System.Text.Json.JsonException("Thrown by test");
        var batchPublisher = Substitute.For<IMessageBatchPublisher, IMessagePublisher>();
        batchPublisher.PublishBatchAsync(Arg.Any<IEnumerable<object>>(), Arg.Any<PublishBatchMetadata>(), Arg.Any<CancellationToken>())
            .ThrowsAsync(exception);

        var bus = await CreateStartedBusAsync((IMessagePublisher)batchPublisher, batchPublisher);

        var thrown = await Should.ThrowAsync<System.Text.Json.JsonException>(
            () => bus.PublishBatchAsync([new SimpleMessage(), new SimpleMessage()], new PublishBatchMetadata(), CancellationToken.None));

        thrown.ShouldBeSameAs(exception);
        await batchPublisher.Received(1).PublishBatchAsync(Arg.Any<IEnumerable<object>>(), Arg.Any<PublishBatchMetadata>(), Arg.Any<CancellationToken>());
    }

    private static async Task<JustSaying.JustSayingBus> CreateStartedBusAsync(
        IMessagePublisher publisher,
        IMessageBatchPublisher batchPublisher)
    {
        var config = Substitute.For<IMessagingConfig, IPublishBatchConfiguration>();
        config.PublishFailureReAttempts.Returns(PublishAttempts);
        config.PublishFailureBackoff.Returns(TimeSpan.Zero);
        ((IPublishBatchConfiguration)config).PublishFailureReAttempts.Returns(PublishAttempts);

        var loggerFactory = TestContext.Current!.OutputWriter.ToLoggerFactory();
        var bus = new JustSaying.JustSayingBus(
            config,
            new NewtonsoftSerializationFactory(),
            new MessageReceivePauseSignal(),
            loggerFactory,
            new TrackingLoggingMonitor(loggerFactory.CreateLogger<TrackingLoggingMonitor>()),
            null);

        if (publisher is not null)
        {
            bus.AddMessagePublisher<SimpleMessage>(publisher);
        }

        if (batchPublisher is not null)
        {
            bus.AddMessageBatchPublisher<SimpleMessage>(batchPublisher);
        }

        await bus.StartAsync(CancellationToken.None);

        return bus;
    }
}
