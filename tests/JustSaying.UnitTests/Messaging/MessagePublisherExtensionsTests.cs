using JustSaying.Messaging;
using JustSaying.Models;
using NSubstitute;

namespace JustSaying.UnitTests.Messaging;

public class MessagePublisherExtensionsTests
{
    [Test]
    public async Task ArgumentsAreCheckedForNull()
    {
        // Arrange
        var message = Substitute.For<Message>();
        var messages = new[] { message };
        var metadata = new PublishMetadata();
        var batchMetadata = new PublishBatchMetadata();

        // Act and Assert
        (await Should.ThrowAsync<ArgumentNullException>(() => (null as IMessagePublisher).PublishAsync(messages))).ParamName.ShouldBe("publisher");
        (await Should.ThrowAsync<ArgumentNullException>(() => (null as IMessagePublisher).PublishAsync(messages, CancellationToken.None))).ParamName.ShouldBe("publisher");
        (await Should.ThrowAsync<ArgumentNullException>(() => (null as IMessagePublisher).PublishAsync(message, metadata))).ParamName.ShouldBe("publisher");
        (await Should.ThrowAsync<ArgumentNullException>(() => (null as IMessagePublisher).PublishAsync(messages, batchMetadata))).ParamName.ShouldBe("publisher");
        (await Should.ThrowAsync<ArgumentNullException>(() => (null as IMessageBatchPublisher).PublishAsync(messages, CancellationToken.None))).ParamName.ShouldBe("publisher");
    }

    [Test]
    public async Task MessagesAreBatchedIfAlsoABatchPublisher()
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

    [Test]
    public async Task MessagesAreSerializedIfNotABatchPublisher()
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
