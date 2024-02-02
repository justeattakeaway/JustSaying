using JustSaying.Messaging;
using JustSaying.Models;
using NSubstitute;

namespace JustSaying.UnitTests.Messaging;

public static class MessagePublisherExtensionsTests
{
    [Fact]
    public static async Task ArgumentsAreCheckedForNull()
    {
        // Arrange
        var message = Substitute.For<Message>();
        var messages = new[] { message };
        var metadata = new PublishMetadata();
        var batchMetadata = new PublishBatchMetadata();

        // Act and Assert
        await Assert.ThrowsAsync<ArgumentNullException>("publisher", () => (null as IMessagePublisher).PublishAsync(messages));
        await Assert.ThrowsAsync<ArgumentNullException>("publisher", () => (null as IMessagePublisher).PublishAsync(messages, CancellationToken.None));
        await Assert.ThrowsAsync<ArgumentNullException>("publisher", () => (null as IMessagePublisher).PublishAsync(message, metadata));
        await Assert.ThrowsAsync<ArgumentNullException>("publisher", () => (null as IMessagePublisher).PublishAsync(messages, batchMetadata));
        await Assert.ThrowsAsync<ArgumentNullException>("publisher", () => (null as IMessageBatchPublisher).PublishAsync(messages, CancellationToken.None));
    }

    [Fact]
    public static async Task MessagesAreBatchedIfAlsoABatchPublisher()
    {
        // Arrange
        var publisher = Substitute.For<IMessagePublisher, IMessageBatchPublisher>();
        var messages = new[] { Substitute.For<Message>(), Substitute.For<Message>() };
        var metadata = new PublishBatchMetadata();
        var cancellationToken = CancellationToken.None;

        // Act
        await publisher.PublishAsync(messages, metadata, cancellationToken);

        // Assert
        await publisher.Received(1).PublishAsync(messages, metadata, cancellationToken);
        await publisher.Received(0).PublishAsync(Arg.Any<Message>(), Arg.Any<PublishMetadata>(), Arg.Any<CancellationToken>());
    }

    [Fact]
    public static async Task MessagesAreSerializedIfNotABatchPublisher()
    {
        // Arrange
        var publisher = Substitute.For<IMessagePublisher>();
        var messages = new[] { Substitute.For<Message>(), Substitute.For<Message>() };
        var metadata = new PublishBatchMetadata();
        var cancellationToken = CancellationToken.None;

        // Act
        await publisher.PublishAsync(messages, metadata, cancellationToken);

        // Assert
        await publisher.Received(1).PublishAsync(messages[0], metadata, cancellationToken);
        await publisher.Received(1).PublishAsync(messages[1], metadata, cancellationToken);
    }
}
