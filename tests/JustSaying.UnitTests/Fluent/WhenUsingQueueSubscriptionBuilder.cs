using JustSaying.AwsTools.QueueCreation;
using JustSaying.Fluent;
using JustSaying.TestingFramework;

namespace JustSaying.UnitTests.Fluent;

public class WhenUsingQueueSubscriptionBuilder
{
    private readonly QueueSubscriptionBuilder<Order> _sut = new();

    [Test]
    [Arguments("")]
    [Arguments(null)]
    public void ShouldThrowArgumentExceptionWhenAddingInvalidTag(string actualTagKey)
    {
        // Act + Assert
        Should.Throw<ArgumentException>(() => _sut.WithTag(actualTagKey));
    }

    [Test]
    public void ShouldThrowArgumentExceptionWhenWriteConfigurationBuilderIsNull()
    {
        // Act + Assert
        Should.Throw<ArgumentNullException>(() => _sut.WithReadConfiguration((Action<SqsReadConfigurationBuilder>) null));
    }

    [Test]
    public void ShouldThrowArgumentExceptionWhenWriteConfigurationIsNull()
    {
        // Act + Assert
        Should.Throw<ArgumentNullException>(() => _sut.WithReadConfiguration((Action<SqsReadConfiguration>) null));
    }
}
