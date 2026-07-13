using JustSaying.AwsTools.MessageHandling;
using JustSaying.Messaging;
using JustSaying.Messaging.Channels.SubscriptionGroups;
using JustSaying.TestingFramework;
using NSubstitute;

namespace JustSaying.UnitTests.Messaging.Channels.SubscriptionGroups;

public class WhenUsingSqsSource
{
    [Test]
    public void ThenSqsQueueCanBeSet()
    {
        // Arrange
        var sut = new SqsSource();
        var queue = Substitute.For<ISqsQueue>();
        queue.QueueName.Returns("test-queue");

        // Act
        sut.SqsQueue = queue;

        // Assert
        sut.SqsQueue.ShouldBe(queue);
    }

    [Test]
    public void ThenMessageConverterCanBeSet()
    {
        // Arrange
        var sut = new SqsSource();
        var converter = Substitute.For<IInboundMessageConverter>();

        // Act
        sut.MessageConverter = converter;

        // Assert
        sut.MessageConverter.ShouldBe(converter);
    }

    [Test]
    public void ThenNameReturnsQueueName()
    {
        // Arrange
        var sut = new SqsSource();
        var queue = Substitute.For<ISqsQueue>();
        queue.QueueName.Returns("my-test-queue");
        sut.SqsQueue = queue;

        // Act
        var name = ((IMessageSource)sut).Name;

        // Assert
        name.ShouldBe("my-test-queue");
    }

    [Test]
    public void ThenNameReturnsEmptyStringWhenQueueIsNull()
    {
        // Arrange
        var sut = new SqsSource();

        // Act
        var name = ((IMessageSource)sut).Name;

        // Assert
        name.ShouldBe(string.Empty);
    }

    [Test]
    public void ThenNameReturnsEmptyStringWhenQueueNameIsNull()
    {
        // Arrange
        var sut = new SqsSource();
        var queue = Substitute.For<ISqsQueue>();
        queue.QueueName.Returns((string)null);
        sut.SqsQueue = queue;

        // Act
        var name = ((IMessageSource)sut).Name;

        // Assert
        name.ShouldBe(string.Empty);
    }

    [Test]
    public void ThenImplementsIMessageSource()
    {
        // Arrange
        var sut = new SqsSource();

        // Assert
        sut.ShouldBeAssignableTo<IMessageSource>();
    }
}
