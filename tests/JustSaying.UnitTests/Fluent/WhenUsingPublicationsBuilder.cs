using JustSaying.Fluent;
using JustSaying.Models;
using JustSaying.TestingFramework;
using NSubstitute;

namespace JustSaying.UnitTests.Fluent;

public class WhenUsingPublicationsBuilder
{
    private readonly MessagingBusBuilder _parentBuilder;
    private readonly PublicationsBuilder _sut;

    public WhenUsingPublicationsBuilder()
    {
        _parentBuilder = new MessagingBusBuilder();
        _sut = new PublicationsBuilder(_parentBuilder);
    }

    [Fact]
    public void ThenBusBuilderPropertyReturnsParent()
    {
        // Assert
        _sut.BusBuilder.ShouldBe(_parentBuilder);
    }

    [Fact]
    public void ThenWithCustomPublicationAddsPublisher()
    {
        // Arrange
        var customPublisher = Substitute.For<IPublicationBuilder<Message>>();

        // Act
        var result = _sut.WithCustomPublication(customPublisher);

        // Assert
        result.ShouldBe(_sut);
    }

    [Fact]
    public void ThenWithCustomPublicationThrowsWhenNull()
    {
        // Act & Assert
        Should.Throw<ArgumentNullException>(() => _sut.WithCustomPublication(null));
    }

    [Fact]
    public void ThenWithQueueReturnsBuilder()
    {
        // Act
        var result = _sut.WithQueue<TestMessage>();

        // Assert
        result.ShouldBe(_sut);
    }

    [Fact]
    public void ThenWithQueueWithConfigureReturnsBuilder()
    {
        // Act
        var result = _sut.WithQueue<TestMessage>(q => { });

        // Assert
        result.ShouldBe(_sut);
    }

    [Fact]
    public void ThenWithQueueThrowsWhenConfigureIsNull()
    {
        // Act & Assert
        Should.Throw<ArgumentNullException>(() => _sut.WithQueue<TestMessage>(null));
    }

    [Fact]
    public void ThenWithQueueArnReturnsBuilder()
    {
        // Act
        var result = _sut.WithQueueArn<TestMessage>("arn:aws:sqs:us-east-1:123456789012:test-queue");

        // Assert
        result.ShouldBe(_sut);
    }

    [Fact]
    public void ThenWithQueueArnThrowsWhenNull()
    {
        // Act & Assert
        Should.Throw<ArgumentNullException>(() => _sut.WithQueueArn<TestMessage>(null));
    }

    [Fact]
    public void ThenWithQueueUrlReturnsBuilder()
    {
        // Act
        var result = _sut.WithQueueUrl<TestMessage>("https://sqs.us-east-1.amazonaws.com/123456789012/test-queue");

        // Assert
        result.ShouldBe(_sut);
    }

    [Fact]
    public void ThenWithQueueUrlThrowsWhenNull()
    {
        // Act & Assert
        Should.Throw<ArgumentNullException>(() => _sut.WithQueueUrl<TestMessage>(null));
    }

    [Fact]
    public void ThenWithQueueUriReturnsBuilder()
    {
        // Act
        var result = _sut.WithQueueUri<TestMessage>(new Uri("https://sqs.us-east-1.amazonaws.com/123456789012/test-queue"));

        // Assert
        result.ShouldBe(_sut);
    }

    [Fact]
    public void ThenWithQueueUriThrowsWhenNull()
    {
        // Act & Assert
        Should.Throw<ArgumentNullException>(() => _sut.WithQueueUri<TestMessage>(null));
    }

    [Fact]
    public void ThenWithTopicReturnsBuilder()
    {
        // Act
        var result = _sut.WithTopic<TestMessage>();

        // Assert
        result.ShouldBe(_sut);
    }

    [Fact]
    public void ThenWithTopicWithConfigureReturnsBuilder()
    {
        // Act
        var result = _sut.WithTopic<TestMessage>(t => { });

        // Assert
        result.ShouldBe(_sut);
    }

    [Fact]
    public void ThenWithTopicThrowsWhenConfigureIsNull()
    {
        // Act & Assert
        Should.Throw<ArgumentNullException>(() => _sut.WithTopic<TestMessage>(null));
    }

    [Fact]
    public void ThenWithTopicArnReturnsBuilder()
    {
        // Act
        var result = _sut.WithTopicArn<TestMessage>("arn:aws:sns:us-east-1:123456789012:test-topic");

        // Assert
        result.ShouldBe(_sut);
    }

    [Fact]
    public void ThenWithTopicArnThrowsWhenNull()
    {
        // Act & Assert
        Should.Throw<ArgumentNullException>(() => _sut.WithTopicArn<TestMessage>(null));
    }

    [Fact]
    public void ThenWithTopicArnWithConfigureReturnsBuilder()
    {
        // Act
        var result = _sut.WithTopicArn<TestMessage>("arn:aws:sns:us-east-1:123456789012:test-topic", t => { });

        // Assert
        result.ShouldBe(_sut);
    }

    private class TestMessage : Message
    {
    }
}
