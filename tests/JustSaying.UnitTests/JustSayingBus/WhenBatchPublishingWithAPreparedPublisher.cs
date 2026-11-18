using JustSaying.AwsTools.MessageHandling;
using JustSaying.Messaging;
using JustSaying.Messaging.Interrogation;
using JustSaying.Models;
using JustSaying.TestingFramework;
using NSubstitute;

namespace JustSaying.UnitTests.JustSayingBus;

public abstract class WhenBatchPublishingWithAPreparedPublisherTestBase : GivenAServiceBus
{
    protected const int PublishAttempts = 3;

    private protected PreparedPublisher Publisher { get; private set; }
    protected List<SimpleMessage> Messages { get; } = [];

    /// <summary>
    /// Decides whether preparing the batch fails, given how many times it has been attempted, including this one.
    /// </summary>
    protected abstract Exception PrepareFailureFor(int attempt);

    protected override void Given()
    {
        base.Given();

        Config = Substitute.For<IMessagingConfig, IPublishBatchConfiguration>();
        Config.PublishFailureBackoff.Returns(TimeSpan.Zero);
        ((IPublishBatchConfiguration)Config).PublishFailureReAttempts.Returns(PublishAttempts);
        RecordAnyExceptionsThrown();

        Publisher = new PreparedPublisher(PrepareFailureFor);

        for (int i = 0; i < 25; i++)
        {
            Messages.Add(new SimpleMessage { Content = $"Message {i}" });
        }
    }

    protected override async Task WhenAsync()
    {
        SystemUnderTest.AddMessageBatchPublisher<SimpleMessage>(Publisher);

        var cts = new CancellationTokenSource(TimeoutPeriod);
        await SystemUnderTest.StartAsync(cts.Token);

        await SystemUnderTest.PublishAsync(Messages, new PublishBatchMetadata(), cts.Token);
    }

    private protected sealed class PreparedPublisher(Func<int, Exception> prepareFailureFor) : IMessageBatchPublisher, IMessagePublisher, IPreparedBatchPublisher
    {
        public int PrepareAttempts { get; private set; }
        public List<IReadOnlyCollection<Message>> Sent { get; } = [];

        public Task<IReadOnlyList<PreparedBatch>> PrepareAsync(IReadOnlyCollection<Message> messages, PublishBatchMetadata metadata, CancellationToken cancellationToken)
        {
            if (prepareFailureFor(++PrepareAttempts) is { } exception)
            {
                throw exception;
            }

            // Pack unevenly, so that the batches cannot be mistaken for the chunks of ten the bus falls back to.
            IReadOnlyList<PreparedBatch> batches =
            [
                .. messages.Chunk(7).Select(chunk => new PreparedBatch(chunk, _ =>
                {
                    Sent.Add(chunk);
                    return Task.CompletedTask;
                }))
            ];

            return Task.FromResult(batches);
        }

        public Task PublishAsync(IEnumerable<Message> messages, PublishBatchMetadata metadata, CancellationToken cancellationToken)
            => throw new NotSupportedException("The bus should prepare the batches and send them itself.");

        public Task PublishAsync(Message message, CancellationToken cancellationToken)
            => throw new NotSupportedException();

        public Task PublishAsync(Message message, PublishMetadata metadata, CancellationToken cancellationToken)
            => throw new NotSupportedException();

        public Task StartAsync(CancellationToken stoppingToken) => Task.CompletedTask;

        public InterrogationResult Interrogate() => InterrogationResult.Empty;
    }
}

public class WhenBatchPublishingWithAPreparedPublisher : WhenBatchPublishingWithAPreparedPublisherTestBase
{
    protected override Exception PrepareFailureFor(int attempt) => null;

    [Test]
    public void TheBatchesThePublisherPreparedAreSent()
    {
        ThrownException.ShouldBeNull();
        Publisher.PrepareAttempts.ShouldBe(1);
        Publisher.Sent.Select(x => x.Count).ShouldBe([7, 7, 7, 4]);
        Publisher.Sent.SelectMany(x => x).ShouldBe(Messages);
    }
}

/// <summary>
/// Preparing sends nothing, so it is safe to retry, and it can fail for reasons that are worth retrying
/// (creating the topic for a dynamic topic name, for one).
/// </summary>
public class WhenPreparingABatchFailsThenSucceeds : WhenBatchPublishingWithAPreparedPublisherTestBase
{
    protected override Exception PrepareFailureFor(int attempt)
        => attempt == 1 ? new TestException("Thrown by test") : null;

    [Test]
    public void PreparingIsRetriedAndEveryMessageIsPublishedOnce()
    {
        ThrownException.ShouldBeNull();
        Publisher.PrepareAttempts.ShouldBe(2);
        Publisher.Sent.SelectMany(x => x).ShouldBe(Messages);
    }
}

public class WhenPreparingABatchFindsAMessageThatIsTooLarge : WhenBatchPublishingWithAPreparedPublisherTestBase
{
    protected override Exception PrepareFailureFor(int attempt)
        => new MessageTooLargeException("Thrown by test");

    [Test]
    public void NothingIsSentAndItIsNotRetried()
    {
        ThrownException.ShouldBeOfType<MessageTooLargeException>();
        Publisher.PrepareAttempts.ShouldBe(1);
        Publisher.Sent.ShouldBeEmpty();
    }
}
