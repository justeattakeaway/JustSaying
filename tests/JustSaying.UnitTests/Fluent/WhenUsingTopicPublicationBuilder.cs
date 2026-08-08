using JustSaying.Fluent;
using JustSaying.TestingFramework;

namespace JustSaying.UnitTests.Fluent;

public class WhenUsingTopicPublicationBuilder
{
    private readonly TopicPublicationBuilder<Order> _sut = new();

    [Test]
    public void ShouldThrowArgumentNullExceptionWhenTopicNameIsNull()
    {
        // Act + Assert
        Should.Throw<ArgumentNullException>(() => _sut.WithTopicName((string)null));
    }

    [Test]
    public void ShouldThrowArgumentNullExceptionWhenCompressionOptionsAreNull()
    {
        // Act + Assert
        Should.Throw<ArgumentNullException>(() => _sut.WithCompression(null));
    }

    [Test]
    [Arguments("")]
    [Arguments(null)]
    public void ShouldThrowArgumentExceptionWhenTopicInfrastructureTagIsInvalid(string tagKey)
    {
        // Act + Assert
        Should.Throw<ArgumentException>(() => Topic.ByConvention(t => t.WithTag(tagKey)));
    }
}
