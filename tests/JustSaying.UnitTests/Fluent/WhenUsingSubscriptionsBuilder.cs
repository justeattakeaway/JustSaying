using JustSaying.Fluent;
using JustSaying.Models;
using JustSaying.TestingFramework;
using NSubstitute;

namespace JustSaying.UnitTests.Fluent;

public class WhenUsingSubscriptionsBuilder
{
    private readonly MessagingBusBuilder _parentBuilder;
    private readonly SubscriptionsBuilder _sut;

    public WhenUsingSubscriptionsBuilder()
    {
        _parentBuilder = new MessagingBusBuilder();
        _sut = new SubscriptionsBuilder(_parentBuilder);
    }

    [Fact]
    public void ThenBusBuilderPropertyReturnsParent()
    {
        // Assert
        _sut.BusBuilder.ShouldBe(_parentBuilder);
    }

    [Fact]
    public void ThenWithCustomSubscriptionAddsSubscriber()
    {
        // Arrange
        var customSubscriber = Substitute.For<ISubscriptionBuilder<Message>>();

        // Act
        var result = _sut.WithCustomSubscription(customSubscriber);

        // Assert
        result.ShouldBe(_sut);
    }

    [Fact]
    public void ThenWithCustomSubscriptionThrowsWhenNull()
    {
        // Act & Assert
        Should.Throw<ArgumentNullException>(() => _sut.WithCustomSubscription(null));
    }

    [Fact]
    public void ThenWithDefaultsConfiguresDefaultSettings()
    {
        // Arrange
        var configured = false;

        // Act
        var result = _sut.WithDefaults(d =>
        {
            configured = true;
            d.WithDefaultConcurrencyLimit(10);
        });

        // Assert
        result.ShouldBe(_sut);
        configured.ShouldBeTrue();
    }

    [Fact]
    public void ThenWithDefaultsThrowsWhenNull()
    {
        // Act & Assert
        Should.Throw<ArgumentNullException>(() => _sut.WithDefaults(null));
    }

    [Fact]
    public void ThenForQueueReturnsBuilder()
    {
        // Act
        var result = _sut.ForQueue<TestMessage>();

        // Assert
        result.ShouldBe(_sut);
    }

    [Fact]
    public void ThenForQueueWithConfigureReturnsBuilder()
    {
        // Act
        var result = _sut.ForQueue<TestMessage>(q => q.WithQueueName("test-queue"));

        // Assert
        result.ShouldBe(_sut);
    }

    [Fact]
    public void ThenForQueueThrowsWhenConfigureIsNull()
    {
        // Act & Assert
        Should.Throw<ArgumentNullException>(() => _sut.ForQueue<TestMessage>(null));
    }

    [Fact]
    public void ThenForQueueArnReturnsBuilder()
    {
        // Act
        var result = _sut.ForQueueArn<TestMessage>("arn:aws:sqs:us-east-1:123456789012:test-queue");

        // Assert
        result.ShouldBe(_sut);
    }

    [Fact]
    public void ThenForQueueArnThrowsWhenNull()
    {
        // Act & Assert
        Should.Throw<ArgumentNullException>(() => _sut.ForQueueArn<TestMessage>(null));
    }

    [Fact]
    public void ThenForQueueArnWithConfigureReturnsBuilder()
    {
        // Act
        var result = _sut.ForQueueArn<TestMessage>("arn:aws:sqs:us-east-1:123456789012:test-queue", q => { });

        // Assert
        result.ShouldBe(_sut);
    }

    [Fact]
    public void ThenForQueueUrlReturnsBuilder()
    {
        // Act
        var result = _sut.ForQueueUrl<TestMessage>("https://sqs.us-east-1.amazonaws.com/123456789012/test-queue");

        // Assert
        result.ShouldBe(_sut);
    }

    [Fact]
    public void ThenForQueueUrlThrowsWhenNull()
    {
        // Act & Assert
        Should.Throw<ArgumentNullException>(() => _sut.ForQueueUrl<TestMessage>(null));
    }

    [Fact]
    public void ThenForQueueUrlWithRegionReturnsBuilder()
    {
        // Act
        var result = _sut.ForQueueUrl<TestMessage>("https://sqs.us-east-1.amazonaws.com/123456789012/test-queue", "us-east-1");

        // Assert
        result.ShouldBe(_sut);
    }

    [Fact]
    public void ThenForQueueUrlWithConfigureReturnsBuilder()
    {
        // Act
        var result = _sut.ForQueueUrl<TestMessage>("https://sqs.us-east-1.amazonaws.com/123456789012/test-queue", null, q => { });

        // Assert
        result.ShouldBe(_sut);
    }

    [Fact]
    public void ThenForQueueUriReturnsBuilder()
    {
        // Act
        var result = _sut.ForQueueUri<TestMessage>(new Uri("https://sqs.us-east-1.amazonaws.com/123456789012/test-queue"));

        // Assert
        result.ShouldBe(_sut);
    }

    [Fact]
    public void ThenForQueueUriThrowsWhenNull()
    {
        // Act & Assert
        Should.Throw<ArgumentNullException>(() => _sut.ForQueueUri<TestMessage>(null));
    }

    [Fact]
    public void ThenForQueueUriWithRegionReturnsBuilder()
    {
        // Act
        var result = _sut.ForQueueUri<TestMessage>(new Uri("https://sqs.us-east-1.amazonaws.com/123456789012/test-queue"), "us-east-1");

        // Assert
        result.ShouldBe(_sut);
    }

    [Fact]
    public void ThenForQueueUriWithConfigureReturnsBuilder()
    {
        // Act
        var result = _sut.ForQueueUri<TestMessage>(new Uri("https://sqs.us-east-1.amazonaws.com/123456789012/test-queue"), null, q => { });

        // Assert
        result.ShouldBe(_sut);
    }

    [Fact]
    public void ThenForTopicReturnsBuilder()
    {
        // Act
        var result = _sut.ForTopic<TestMessage>();

        // Assert
        result.ShouldBe(_sut);
    }

    [Fact]
    public void ThenForTopicWithConfigureReturnsBuilder()
    {
        // Act
        var result = _sut.ForTopic<TestMessage>(t => t.WithQueueName("test-queue"));

        // Assert
        result.ShouldBe(_sut);
    }

    [Fact]
    public void ThenForTopicThrowsWhenConfigureIsNull()
    {
        // Act & Assert
        Should.Throw<ArgumentNullException>(() => _sut.ForTopic<TestMessage>(null));
    }

    [Fact]
    public void ThenForTopicWithNameReturnsBuilder()
    {
        // Act
        var result = _sut.ForTopic<TestMessage>("custom-topic", t => t.WithQueueName("test-queue"));

        // Assert
        result.ShouldBe(_sut);
    }

    [Fact]
    public void ThenForTopicWithNameThrowsWhenConfigureIsNull()
    {
        // Act & Assert
        Should.Throw<ArgumentNullException>(() => _sut.ForTopic<TestMessage>("custom-topic", null));
    }

    [Fact]
    public void ThenForTopicWithNameThrowsWhenTopicNameIsNull()
    {
        // Act & Assert
        Should.Throw<ArgumentNullException>(() => _sut.ForTopic<TestMessage>(null, t => { }));
    }

    [Fact]
    public void ThenWithSubscriptionGroupAddsOrUpdatesGroup()
    {
        // Arrange
        var configured = false;

        // Act
        var result = _sut.WithSubscriptionGroup("test-group", g =>
        {
            configured = true;
            g.WithConcurrencyLimit(5);
        });

        // Assert
        result.ShouldBe(_sut);
        configured.ShouldBeTrue();
    }

    [Fact]
    public void ThenWithSubscriptionGroupThrowsWhenGroupNameIsNull()
    {
        // Act & Assert
        Should.Throw<ArgumentException>(() => _sut.WithSubscriptionGroup(null, g => { }));
    }

    [Fact]
    public void ThenWithSubscriptionGroupThrowsWhenGroupNameIsEmpty()
    {
        // Act & Assert
        Should.Throw<ArgumentException>(() => _sut.WithSubscriptionGroup("", g => { }));
    }

    [Fact]
    public void ThenWithSubscriptionGroupThrowsWhenActionIsNull()
    {
        // Act & Assert
        Should.Throw<ArgumentNullException>(() => _sut.WithSubscriptionGroup("test-group", null));
    }

    [Fact]
    public void ThenWithSubscriptionGroupCanUpdateExistingGroup()
    {
        // Arrange
        var firstCallCount = 0;
        var secondCallCount = 0;

        // Act
        _sut.WithSubscriptionGroup("test-group", g => { firstCallCount++; });
        _sut.WithSubscriptionGroup("test-group", g => { secondCallCount++; });

        // Assert
        firstCallCount.ShouldBe(1);
        secondCallCount.ShouldBe(1);
    }

    private class TestMessage : Message
    {
    }
}
