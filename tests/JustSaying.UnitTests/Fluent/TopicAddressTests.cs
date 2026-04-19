using JustSaying.Fluent;

namespace JustSaying.UnitTests.Fluent;

public class TopicAddressTests
{
    [Test]
    public void ParsingEmptyArnThrows()
    {
        Should.Throw<ArgumentException>(() => TopicAddress.FromArn("")).ParamName.ShouldBe("topicArn");
    }

    [Test]
    public void ParsingNullArnThrows()
    {
        Should.Throw<ArgumentException>(() => TopicAddress.FromArn(null)).ParamName.ShouldBe("topicArn");
    }

    [Test]
    public void ValidArnCanBeParsed()
    {
        var ta = TopicAddress.FromArn("arn:aws:sns:eu-west-1:111122223333:topic1");

        ta.TopicArn.ShouldBe("arn:aws:sns:eu-west-1:111122223333:topic1");
    }

    [Test]
    public void ArnForWrongServiceThrows()
    {
        Should.Throw<ArgumentException>(() => TopicAddress.FromArn("arn:aws:sqs:eu-west-1:111122223333:queue1")).ParamName.ShouldBe("topicArn");
    }
}