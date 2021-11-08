using JustSaying.AwsTools.QueueCreation;
using JustSaying.Fluent;
using JustSaying.TestingFramework;
using Shouldly;
using Xunit;

namespace JustSaying.UnitTests.Fluent;

public class WhenUsingQueueSubscriptionBuilder
{
    private readonly QueueSubscriptionBuilder<Order> _sut;

    public WhenUsingQueueSubscriptionBuilder()
    {
        _sut = new QueueSubscriptionBuilder<Order>();
    }

    [Theory]
    [InlineData("")]
    [InlineData(null)]
    public void ShouldThrowArgumentExceptionWhenAddingInvalidTag(string actualTagKey)
    {
        // Act + Assert
        Should.Throw<ArgumentException>(() => _sut.WithTag(actualTagKey));
    }

    [Fact]
    public void ShouldThrowArgumentExceptionWhenWriteConfigurationBuilderIsNull()
    {
        // Act + Assert
        Should.Throw<ArgumentNullException>(() => _sut.WithReadConfiguration((Action<SqsReadConfigurationBuilder>) null));
    }

    [Fact]
    public void ShouldThrowArgumentExceptionWhenWriteConfigurationIsNull()
    {
        // Act + Assert
        Should.Throw<ArgumentNullException>(() => _sut.WithReadConfiguration((Action<SqsReadConfiguration>) null));
    }
}