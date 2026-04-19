using JustSaying.Fluent;
using JustSaying.Naming;
using JustSaying.TestingFramework;

namespace JustSaying.UnitTests.Fluent;

public class AccountAddressProviderTests
{
    [Test]
    public void CanGetAccountQueueByName()
    {
        var sut = new AccountAddressProvider("123456789012", "eu-west-1");
        var address = sut.GetQueueUri("queue1");

        address.ShouldBe(new Uri(" https://sqs.eu-west-1.amazonaws.com/123456789012/queue1"));
    }

    [Test]
    public void CanGetAccountTopicByName()
    {
        var sut = new AccountAddressProvider("123456789012", "eu-west-1");
        var address = sut.GetTopicArn("topic1");

        address.ShouldBe("arn:aws:sns:eu-west-1:123456789012:topic1");
    }

    [Test]
    public void CanGetAccountQueueByDefaultConvention()
    {
        var sut = new AccountAddressProvider("123456789012", "eu-west-1");
        var address = sut.GetQueueUriByConvention<Order>();

        address.ShouldBe(new Uri(" https://sqs.eu-west-1.amazonaws.com/123456789012/order"));
    }

    [Test]
    public void CanGetAccountTopicByDefaultConvention()
    {
        var sut = new AccountAddressProvider("123456789012", "eu-west-1");
        var address = sut.GetTopicArnByConvention<Order>();

        address.ShouldBe("arn:aws:sns:eu-west-1:123456789012:order");
    }

    [Test]
    public void CanGetAccountQueueByCustomConvention()
    {
        var convention = new ManualNamingConvention("adhoc-queue-name", null);
        var sut = new AccountAddressProvider("123456789012", "eu-west-1", convention, null);
        var address = sut.GetQueueUriByConvention<Order>();

        address.ShouldBe(new Uri(" https://sqs.eu-west-1.amazonaws.com/123456789012/adhoc-queue-name"));
    }

    [Test]
    public void CanGetAccountTopicByCustomConvention()
    {
        var convention = new ManualNamingConvention(null, "adhoc-topic-name");
        var sut = new AccountAddressProvider("123456789012", "eu-west-1", null, convention);
        var address = sut.GetTopicArnByConvention<Order>();

        address.ShouldBe("arn:aws:sns:eu-west-1:123456789012:adhoc-topic-name");
    }

    private class ManualNamingConvention(string queueName, string topicName) : IQueueNamingConvention, ITopicNamingConvention
    {
        private readonly string _queueName = queueName;
        private readonly string _topicName = topicName;

        public string QueueName<T>() => _queueName;
        public string TopicName<T>() => _topicName;
    }
}