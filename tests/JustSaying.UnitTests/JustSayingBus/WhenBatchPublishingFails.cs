using JustSaying.AwsTools.MessageHandling;
using JustSaying.Messaging;
using JustSaying.Messaging.Interrogation;
using JustSaying.Models;
using JustSaying.TestingFramework;
using NSubstitute;

namespace JustSaying.UnitTests.JustSayingBus;

public abstract class WhenBatchPublishingFailsTestBase : GivenAServiceBus
{
    protected const int PublishAttempts = 3;
    protected const int MessageCount = 25;

    private protected RecordingBatchPublisher Publisher { get; private set; }
    protected List<SimpleMessage> Messages { get; } = [];

    /// <summary>
    /// Decides whether a call to the publisher fails, given the chunk being published and how
    /// many times that chunk has been attempted so far, including this attempt.
    /// </summary>
    protected abstract Exception FailureFor(IReadOnlyList<Message> chunk, int attempt);

    protected override void Given()
    {
        base.Given();

        Config = Substitute.For<IMessagingConfig, IPublishBatchConfiguration>();
        Config.PublishFailureBackoff.Returns(TimeSpan.Zero);
        ((IPublishBatchConfiguration)Config).PublishFailureReAttempts.Returns(PublishAttempts);
        RecordAnyExceptionsThrown();

        Publisher = new RecordingBatchPublisher(FailureFor);

        for (int i = 0; i < MessageCount; i++)
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

    protected bool IsSecondChunk(IReadOnlyList<Message> chunk) => chunk[0] == Messages[10];

    private protected sealed class RecordingBatchPublisher(Func<IReadOnlyList<Message>, int, Exception> failureFor) : IMessageBatchPublisher, IMessagePublisher
    {
        private readonly Dictionary<Message, int> _attemptsByFirstMessage = [];

        public List<IReadOnlyList<Message>> Attempts { get; } = [];
        public List<Message> Published { get; } = [];

        public Task PublishAsync(IEnumerable<Message> messages, PublishBatchMetadata metadata, CancellationToken cancellationToken)
        {
            var chunk = messages.ToList();
            Attempts.Add(chunk);

            _attemptsByFirstMessage.TryGetValue(chunk[0], out int attempt);
            _attemptsByFirstMessage[chunk[0]] = ++attempt;

            if (failureFor(chunk, attempt) is { } exception)
            {
                throw exception;
            }

            Published.AddRange(chunk);
            return Task.CompletedTask;
        }

        // The bus only accepts a batch publish for a type that also has a publisher registered.
        public Task PublishAsync(Message message, CancellationToken cancellationToken)
            => throw new NotSupportedException();

        public Task PublishAsync(Message message, PublishMetadata metadata, CancellationToken cancellationToken)
            => throw new NotSupportedException();

        public Task StartAsync(CancellationToken stoppingToken) => Task.CompletedTask;

        public InterrogationResult Interrogate() => InterrogationResult.Empty;
    }
}

/// <summary>
/// A failed chunk should be retried on its own. Retrying the whole batch would publish the chunks
/// that were already accepted a second time.
/// </summary>
public class WhenOneChunkOfABatchFailsThenSucceeds : WhenBatchPublishingFailsTestBase
{
    protected override Exception FailureFor(IReadOnlyList<Message> chunk, int attempt)
        => IsSecondChunk(chunk) && attempt == 1 ? new TestException("Thrown by test") : null;

    [Test]
    public void NoExceptionIsThrown()
    {
        ThrownException.ShouldBeNull();
    }

    [Test]
    public void OnlyTheFailedChunkIsRetried()
    {
        // Three chunks (10, 10 and 5), plus one retry of the second.
        Publisher.Attempts.Count.ShouldBe(4);
        Publisher.Attempts[1].ShouldBe(Publisher.Attempts[2]);
    }

    [Test]
    public void EveryMessageIsPublishedExactlyOnce()
    {
        Publisher.Published.ShouldBe(Messages);
    }
}

public class WhenOneChunkOfABatchKeepsFailing : WhenBatchPublishingFailsTestBase
{
    protected override Exception FailureFor(IReadOnlyList<Message> chunk, int attempt)
        => IsSecondChunk(chunk) ? new TestException("Thrown by test") : null;

    [Test]
    public void TheExceptionIsThrown()
    {
        ThrownException.ShouldBeOfType<TestException>();
    }

    [Test]
    public void TheFailedChunkIsAttemptedTheConfiguredNumberOfTimes()
    {
        Publisher.Attempts.Count(IsSecondChunk).ShouldBe(PublishAttempts);
    }

    [Test]
    public void TheFirstChunkIsPublishedExactlyOnce()
    {
        Publisher.Published.ShouldBe(Messages.Take(10));
    }

    [Test]
    public void LaterChunksAreNotAttempted()
    {
        Publisher.Attempts.Count.ShouldBe(1 + PublishAttempts);
    }
}

/// <summary>
/// A message that is too large will be too large on every attempt, so retrying only delays the failure.
/// </summary>
public class WhenABatchContainsAMessageThatIsTooLarge : WhenBatchPublishingFailsTestBase
{
    protected override Exception FailureFor(IReadOnlyList<Message> chunk, int attempt)
        => new MessageTooLargeException("Thrown by test");

    [Test]
    public void TheExceptionIsThrown()
    {
        ThrownException.ShouldBeOfType<MessageTooLargeException>();
    }

    [Test]
    public void ThePublishIsNotRetried()
    {
        Publisher.Attempts.Count.ShouldBe(1);
    }
}
