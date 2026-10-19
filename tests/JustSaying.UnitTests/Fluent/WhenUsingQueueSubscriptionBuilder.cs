using JustSaying.Fluent;
using JustSaying.TestingFramework;

namespace JustSaying.UnitTests.Fluent;

public class WhenUsingQueueSubscriptionBuilder
{
    private readonly QueueSubscriptionBuilder<Order> _sut = new();

    [Test]
    public void ShouldThrowArgumentNullExceptionWhenQueueNameIsNull()
    {
        // Act + Assert
        Should.Throw<ArgumentNullException>(() => _sut.WithQueueName(null));
    }

    [Test]
    [Arguments("")]
    [Arguments(null)]
    public void ShouldThrowArgumentExceptionWhenSubscriptionGroupIsInvalid(string subscriptionGroupName)
    {
        // Act + Assert
        Should.Throw<ArgumentException>(() => _sut.WithSubscriptionGroup(subscriptionGroupName));
    }
}
