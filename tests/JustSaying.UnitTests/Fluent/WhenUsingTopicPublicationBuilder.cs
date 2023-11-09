using JustSaying.AwsTools.QueueCreation;
using JustSaying.Fluent;
using JustSaying.TestingFramework;

namespace JustSaying.UnitTests.Fluent;

public class WhenUsingTopicPublicationBuilder
{
    private readonly TopicPublicationBuilder<Order> _sut = new();

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
        Should.Throw<ArgumentNullException>(() => _sut.WithWriteConfiguration((Action<SnsWriteConfigurationBuilder>) null));
    }

    [Fact]
    public void ShouldThrowArgumentExceptionWhenWriteConfigurationIsNull()
    {
        // Act + Assert
        Should.Throw<ArgumentNullException>(() => _sut.WithWriteConfiguration((Action<SnsWriteConfiguration>) null));
    }
}
