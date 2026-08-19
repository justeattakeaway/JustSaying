using JustSaying.Fluent;
using JustSaying.TestingFramework;

namespace JustSaying.UnitTests.Fluent;

public class WhenUsingTopicSubscriberBuilder
{
    private readonly TopicSubscriptionBuilder<Order> _sut = new();

    [Test]
    public void ShouldThrowArgumentNullExceptionWhenQueueIsNull()
    {
        // Act + Assert
        Should.Throw<ArgumentNullException>(() => _sut.WithQueue(null));
    }

    [Test]
    [Arguments("")]
    [Arguments(null)]
    public void ShouldThrowArgumentExceptionWhenQueueInfrastructureTagIsInvalid(string tagKey)
    {
        // Act + Assert
        Should.Throw<ArgumentException>(() => QueueDestination.ByConvention(q => q.WithTag(tagKey)));
    }
}
