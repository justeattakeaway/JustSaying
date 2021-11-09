using JustSaying.Fluent;

namespace JustSaying.UnitTests.Fluent;

public class TopicAddressTests
{
    [Fact]
    public void ParsingEmptyArnThrows()
    {
        Assert.Throws<ArgumentException>("topicArn", () => TopicAddress.FromArn(""));
    }

    [Fact]
    public void ParsingNullArnThrows()
    {
        Assert.Throws<ArgumentException>("topicArn", () => TopicAddress.FromArn(null));
    }

    [Fact]
    public void ValidArnCanBeParsed()
    {
        var ta = TopicAddress.FromArn("arn:aws:sns:eu-west-1:111122223333:topic1");

        Assert.Equal("arn:aws:sns:eu-west-1:111122223333:topic1", ta.TopicArn);
    }

    [Fact]
    public void ArnForWrongServiceThrows()
    {
        Assert.Throws<ArgumentException>("topicArn", () => TopicAddress.FromArn("arn:aws:sqs:eu-west-1:111122223333:queue1"));
    }
}