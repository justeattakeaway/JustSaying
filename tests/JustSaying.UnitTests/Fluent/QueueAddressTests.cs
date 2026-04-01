using JustSaying.Fluent;

namespace JustSaying.UnitTests.Fluent;

public class QueueAddressTests
{
    [Test]
    public void ParsingEmptyArnThrows()
    {
        Should.Throw<ArgumentException>(() => QueueAddress.FromArn("")).ParamName.ShouldBe("queueArn");
    }

    [Test]
    public void ParsingNullArnThrows()
    {
        Should.Throw<ArgumentException>(() => QueueAddress.FromArn(null)).ParamName.ShouldBe("queueArn");
    }

    [Test]
    public void ValidArnCanBeParsed()
    {
        var qa = QueueAddress.FromArn("arn:aws:sqs:eu-west-1:111122223333:queue1");

        qa.QueueUrl.AbsoluteUri.ShouldBe("https://sqs.eu-west-1.amazonaws.com/111122223333/queue1");
        qa.RegionName.ShouldBe("eu-west-1");
    }

    [Test]
    public void ArnForWrongServiceThrows()
    {
        Should.Throw<ArgumentException>(() => QueueAddress.FromArn("arn:aws:sns:eu-west-1:111122223333:queue1")).ParamName.ShouldBe("queueArn");
    }

    [Test]
    public void ValidUrlCanBeParsed()
    {
        var qa = QueueAddress.FromUrl("https://sqs.eu-west-1.amazonaws.com/111122223333/queue1");

        qa.QueueUrl.AbsoluteUri.ShouldBe("https://sqs.eu-west-1.amazonaws.com/111122223333/queue1");
        qa.RegionName.ShouldBe("eu-west-1");
    }

    [Test]
    public void UppercaseUrlCanBeParsed()
    {
        var qa = QueueAddress.FromUrl("HTTPS://SQS.EU-WEST-1.AMAZONAWS.COM/111122223333/Queue1");

        // Queue name is case-sensitive.
        qa.QueueUrl.AbsoluteUri.ShouldBe("https://sqs.eu-west-1.amazonaws.com/111122223333/Queue1");
        qa.RegionName.ShouldBe("eu-west-1");
    }

    [Test]
    public void LocalStackUrlWithoutRegionHashUnknownRegion()
    {
        var qa = QueueAddress.FromUrl("http://localhost:4576/111122223333/queue1");

        qa.RegionName.ShouldBe("unknown");
    }

    [Test]
    public void LocalStackUrlWithRegionCanBeParsed()
    {
        var qa = QueueAddress.FromUrl("http://localhost:4576/111122223333/queue1","us-east-1");

        qa.QueueUrl.AbsoluteUri.ShouldBe("http://localhost:4576/111122223333/queue1");
        qa.RegionName.ShouldBe("us-east-1");
    }

    [Test]
    public void EmptyUrlThrows()
    {
        Should.Throw<ArgumentException>(() => QueueAddress.FromUrl("")).ParamName.ShouldBe("queueUrl");
    }
}