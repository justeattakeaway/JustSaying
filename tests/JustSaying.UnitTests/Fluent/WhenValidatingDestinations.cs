using JustSaying.AwsTools.QueueCreation;
using JustSaying.Fluent;

namespace JustSaying.UnitTests.Fluent;

public class WhenValidatingDestinations
{
    [Test]
    [Arguments(null)]
    [Arguments("")]
    [Arguments("   ")]
    public void ABlankQueueNameIsRejected(string name)
    {
        Should.Throw<ArgumentException>(() => QueueDestination.Named(name)).ParamName.ShouldBe("name");
        Should.Throw<ArgumentException>(() => QueueDestination.Named(name, _ => { })).ParamName.ShouldBe("name");
    }

    [Test]
    [Arguments(null)]
    [Arguments("")]
    [Arguments("   ")]
    public void ABlankTopicNameIsRejected(string name)
    {
        Should.Throw<ArgumentException>(() => TopicDestination.Named(name)).ParamName.ShouldBe("name");
        Should.Throw<ArgumentException>(() => TopicDestination.Named(name, _ => { })).ParamName.ShouldBe("name");
    }

    [Test]
    public void AFifoQueueIsRejected()
    {
        Should.Throw<ArgumentException>(() => QueueDestination.Named("orders.fifo"))
            .Message.ShouldContain("FIFO queues are not supported");
    }

    [Test]
    public void AFifoTopicIsRejected()
    {
        Should.Throw<ArgumentException>(() => TopicDestination.Named("orders.fifo"))
            .Message.ShouldContain("FIFO topics are not supported");
    }

    [Test]
    [Arguments("orders queue")]
    [Arguments("orders.queue")]
    [Arguments("orders/queue")]
    public void AQueueNameWithInvalidCharactersIsRejected(string name)
    {
        Should.Throw<ArgumentException>(() => QueueDestination.Named(name)).Message.ShouldContain("alphanumeric");
    }

    [Test]
    public void ATopicNameWithInvalidCharactersIsRejected()
    {
        Should.Throw<ArgumentException>(() => TopicDestination.Named("orders.topic")).Message.ShouldContain("alphanumeric");
    }

    [Test]
    public void AQueueNameIsLimitedSoItsErrorQueueNameFits()
    {
        var longest = new string('q', 74);
        var tooLong = new string('q', 75);

        QueueDestination.Named(longest).ShouldNotBeNull();
        Should.Throw<ArgumentException>(() => QueueDestination.Named(tooLong)).Message.ShouldContain($"{tooLong}_error");
        Should.Throw<ArgumentException>(() => QueueDestination.Named(tooLong, q => q.WithVisibilityTimeout(TimeSpan.FromSeconds(10))));
    }

    [Test]
    public void AQueueWithNoErrorQueueCanUseTheWholeNameLength()
    {
        QueueDestination.Named(new string('q', 80), q => q.WithNoErrorQueue()).ShouldNotBeNull();
        Should.Throw<ArgumentException>(() => QueueDestination.Named(new string('q', 81), q => q.WithNoErrorQueue()));
    }

    [Test]
    public void ATopicNameIsLimitedTo256Characters()
    {
        TopicDestination.Named(new string('t', 256)).ShouldNotBeNull();
        Should.Throw<ArgumentException>(() => TopicDestination.Named(new string('t', 257)));
    }

    [Test]
    [Arguments(0)]
    [Arguments(-5)]
    [Arguments(43201)]
    public void AnOutOfRangeVisibilityTimeoutIsRejected(int seconds)
    {
        var configuration = new SqsBasicConfiguration
        {
            QueueName = "orders",
            VisibilityTimeout = TimeSpan.FromSeconds(seconds),
        };

        Should.Throw<ConfigurationErrorsException>(configuration.Validate).Message.ShouldContain("VisibilityTimeout");
    }

    [Test]
    public void TheMaximumVisibilityTimeoutIsAccepted()
    {
        var configuration = new SqsBasicConfiguration
        {
            QueueName = "orders",
            VisibilityTimeout = TimeSpan.FromHours(12),
        };

        configuration.Validate();
    }

    [Test]
    [Arguments(0)]
    [Arguments(-1)]
    [Arguments(1001)]
    public void AnOutOfRangeRetryCountIsRejected(int retries)
    {
        var configuration = new SqsBasicConfiguration
        {
            QueueName = "orders",
            RetryCountBeforeSendingToErrorQueue = retries,
        };

        Should.Throw<ConfigurationErrorsException>(configuration.Validate).Message.ShouldContain("RetryCountBeforeSendingToErrorQueue");
    }

    [Test]
    public void TheRetryCountIsIgnoredWithNoErrorQueue()
    {
        var configuration = new SqsBasicConfiguration
        {
            QueueName = "orders",
            RetryCountBeforeSendingToErrorQueue = 0,
            ErrorQueueOptOut = true,
        };

        configuration.Validate();
    }

    [Test]
    public void AConventionQueueNameIsValidatedWhenTheBusIsBuilt()
    {
        var configuration = new SqsBasicConfiguration
        {
            QueueName = new string('q', 80),
        };

        Should.Throw<ConfigurationErrorsException>(() => configuration.Validate("queue subscription for 'Order' to queue 'qqq'"))
            .Message.ShouldContain("in the queue subscription for 'Order'");
    }
}
