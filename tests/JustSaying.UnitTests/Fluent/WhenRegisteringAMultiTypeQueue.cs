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

    [Test]
    public void ShouldThrowWhenSubscribingToANullTopic()
    {
        // Act + Assert
        Should.Throw<ArgumentNullException>(() => new MessagingBusBuilder()
            .Subscriptions(s => s.ForQueue("orders", q => q.Handling<Order>().SubscribeToTopic(null))))
            .ParamName.ShouldBe("topic");
    }

    [Test]
    public void ShouldThrowWhenSubscribingToATopicNamedByConvention()
    {
        // Act + Assert - with no message type there is nothing to name the topic after.
        var exception = Should.Throw<ArgumentException>(() => new MessagingBusBuilder()
            .Subscriptions(s => s.ForQueue("orders", q => q.Handling<Order>().SubscribeToTopic(TopicDestination.ByConvention()))));

        exception.ParamName.ShouldBe("topic");
        exception.Message.ShouldContain("SubscribeToTopic<T>()");
    }

    [Test]
    public void ShouldThrowWhenSubscribingToATopicByArn()
    {
        // Act + Assert
        var exception = Should.Throw<ArgumentException>(() => new MessagingBusBuilder()
            .Subscriptions(s => s.ForQueue("orders", q => q
                .Handling<Order>()
                .SubscribeToTopic(TopicDestination.FromArn("arn:aws:sns:eu-west-1:123456789012:orders")))));

        exception.ParamName.ShouldBe("topic");
        exception.Message.ShouldContain("cannot target a topic by ARN");
    }

    [Test]
    public void ShouldThrowWhenSubscribingToATopicWithInfrastructure()
    {
        // Act + Assert
        var exception = Should.Throw<ArgumentException>(() => new MessagingBusBuilder()
            .Subscriptions(s => s.ForQueue("orders", q => q
                .Handling<Order>()
                .SubscribeToTopic(TopicDestination.Named("orders", t => t.WithTag("team"))))));

        exception.ParamName.ShouldBe("topic");
        exception.Message.ShouldContain("configure it on the publication side");
    }

    [Test]
    [Arguments(null)]
    [Arguments("")]
    [Arguments(" ")]
    public void ShouldThrowWhenTheFilterPolicyIsBlank(string filterPolicy)
    {
        // Act + Assert
        Should.Throw<ArgumentException>(() => new MessagingBusBuilder()
            .Subscriptions(s => s.ForQueue("orders", q => q.Handling<Order>().SubscribeToTopic(TopicDestination.Named("orders"), filterPolicy))))
            .ParamName.ShouldBe("filterPolicy");

        Should.Throw<ArgumentException>(() => new MessagingBusBuilder()
            .Subscriptions(s => s.ForQueue("orders", q => q.Handling<Order>().SubscribeToTopic<Order>(filterPolicy))))
            .ParamName.ShouldBe("filterPolicy");
    }
}
