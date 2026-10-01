using JustSaying.Fluent;
using JustSaying.TestingFramework;

namespace JustSaying.UnitTests.Fluent;

public class WhenRegisteringAMultiTypeQueue
{
    [Test]
    public void ShouldThrowWhenTheQueueIsNamedByConvention()
    {
        // Act + Assert
        var exception = Should.Throw<ArgumentException>(() => new MessagingBusBuilder()
            .Subscriptions(s => s.ForQueue(QueueDestination.ByConvention(), q => q.Handling<Order>())));

        exception.ParamName.ShouldBe("destination");
        exception.Message.ShouldStartWith("A multi-type queue needs an explicit name or address");
    }
}
