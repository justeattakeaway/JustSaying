using System;
using JustSaying.Fluent;
using Xunit;

namespace JustSaying.UnitTests.Fluent
{
    public class QueueAddressTests
    {
        [Fact]
        public void TwoNoneInstancesAreConsideredEqual()
        {
            var qa1 = QueueAddress.None;
            var qa2 = QueueAddress.None;

            Assert.Equal(qa1, qa2);
        }

        [Fact]
        public void ParsingEmptyArnThrows()
        {
            Assert.Throws<ArgumentException>("queueArn",() => QueueAddress.FromArn(""));
        }

        [Fact]
        public void ParsingNullArnThrows()
        {
            Assert.Throws<ArgumentException>("queueArn", () => QueueAddress.FromArn(null));
        }

        [Fact]
        public void ValidArnCanBeParsed()
        {
            var qa = QueueAddress.FromArn("arn:aws:sqs:eu-west-1:111122223333:queue1");

            Assert.Equal("https://sqs.eu-west-1.amazonaws.com/111122223333/queue1", qa.QueueUrl.AbsoluteUri);
            Assert.Equal("eu-west-1", qa.RegionName);
        }

        [Fact]
        public void ArnForWrongServiceThrows()
        {
            Assert.Throws<ArgumentException>("queueArn", () => QueueAddress.FromArn("arn:aws:sns:eu-west-1:111122223333:queue1"));
        }

        [Fact]
        public void ValidUrlCanBeParsed()
        {
            var qa = QueueAddress.FromUrl("https://sqs.eu-west-1.amazonaws.com/111122223333/queue1");

            Assert.Equal("https://sqs.eu-west-1.amazonaws.com/111122223333/queue1", qa.QueueUrl.AbsoluteUri);
            Assert.Equal("eu-west-1", qa.RegionName);
        }

        [Fact]
        public void UppercaseUrlCanBeParsed()
        {
            var qa = QueueAddress.FromUrl("HTTPS://SQS.EU-WEST-1.AMAZONAWS.COM/111122223333/Queue1");

            // Queue name is case-sensitive.
            Assert.Equal("https://sqs.eu-west-1.amazonaws.com/111122223333/Queue1", qa.QueueUrl.AbsoluteUri);
            Assert.Equal("eu-west-1", qa.RegionName);
        }

        [Fact]
        public void LocalStackUrlWithoutRegionHashUnknownRegion()
        {
            var qa = QueueAddress.FromUrl("http://localhost:4576/111122223333/queue1");

            Assert.Equal("unknown", qa.RegionName);
        }

        [Fact]
        public void LocalStackUrlWithRegionCanBeParsed()
        {
            var qa = QueueAddress.FromUrl("http://localhost:4576/111122223333/queue1","us-east-1");

            Assert.Equal("http://localhost:4576/111122223333/queue1", qa.QueueUrl.AbsoluteUri);
            Assert.Equal("us-east-1", qa.RegionName);
        }

        [Fact]
        public void EmptyUrlThrows()
        {
            Assert.Throws<ArgumentException>("queueUrl", () => QueueAddress.FromUrl(""));
        }
    }
}
