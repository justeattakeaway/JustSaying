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

    [Test]
    public void NullUriThrows()
    {
        Should.Throw<ArgumentNullException>(() => QueueAddress.FromUri(null)).ParamName.ShouldBe("queueUrl");
    }

    [Test]
    [Arguments("https://sqs-fips.us-east-1.amazonaws.com/111122223333/queue1", "us-east-1")]
    [Arguments("https://vpce-0123456789abcdef0-abcdefgh.sqs.eu-west-1.vpce.amazonaws.com/111122223333/queue1", "eu-west-1")]
    [Arguments("https://eu-west-1.queue.amazonaws.com/111122223333/queue1", "eu-west-1")]
    [Arguments("https://queue.amazonaws.com/111122223333/queue1", "us-east-1")]
    [Arguments("https://sqs.us-gov-west-1.amazonaws.com/111122223333/queue1", "us-gov-west-1")]
    [Arguments("https://sqs.cn-north-1.amazonaws.com.cn/111122223333/queue1", "cn-north-1")]
    [Arguments("http://sqs.eu-west-2.localhost.localstack.cloud:4566/000000000000/queue1", "eu-west-2")]
    public void AlternativeEndpointUrlsCanBeParsed(string url, string expectedRegion)
    {
        var qa = QueueAddress.FromUrl(url);

        qa.QueueUrl.AbsoluteUri.ShouldBe(url);
        qa.RegionName.ShouldBe(expectedRegion);
    }

    [Test]
    public void AlternativeEndpointUrlCanBeParsedWithAMatchingRegion()
    {
        var qa = QueueAddress.FromUrl("https://vpce-0123456789abcdef0-abcdefgh.sqs.eu-west-1.vpce.amazonaws.com/111122223333/queue1", "eu-west-1");

        qa.RegionName.ShouldBe("eu-west-1");
    }

    [Test]
    public void LocalStackHostnameUrlWithRegionCanBeParsed()
    {
        var qa = QueueAddress.FromUrl("http://localhost.localstack.cloud:4566/000000000000/queue1", "us-west-2");

        qa.RegionName.ShouldBe("us-west-2");
    }

    [Test]
    public void ContradictoryRegionThrows()
    {
        Should.Throw<ArgumentException>(() => QueueAddress.FromUrl("https://sqs.eu-west-1.amazonaws.com/111122223333/queue1", "us-east-1"))
            .ParamName.ShouldBe("regionName");
    }

    [Test]
    public void NonSqsAwsUrlThrows()
    {
        var exception = Should.Throw<ArgumentException>(() => QueueAddress.FromUrl("https://sns.eu-west-1.amazonaws.com/111122223333/queue1"));

        exception.ParamName.ShouldBe("queueUrl");
        exception.Message.ShouldNotContain("ARN");
    }

    [Test]
    [Arguments("https://sqs.eu-west-1.amazonaws.com/queue1")]
    [Arguments("https://sqs.eu-west-1.amazonaws.com/")]
    [Arguments("https://sqs.eu-west-1.amazonaws.com/111122223333/queue1/extra")]
    public void UrlWithoutAnAccountAndQueueNameThrows(string url)
    {
        Should.Throw<ArgumentException>(() => QueueAddress.FromUrl(url)).ParamName.ShouldBe("queueUrl");
    }

    [Test]
    [Arguments("arn:aws:sqs::111122223333:queue1")]
    [Arguments("arn:aws:sqs:eu-west-1::queue1")]
    public void IncompleteArnThrows(string arn)
    {
        Should.Throw<ArgumentException>(() => QueueAddress.FromArn(arn)).ParamName.ShouldBe("queueArn");
    }
}