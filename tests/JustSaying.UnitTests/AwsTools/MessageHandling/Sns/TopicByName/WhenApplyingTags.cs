using System.Collections.Generic;
using System.Threading.Tasks;
using Amazon.SimpleNotificationService.Model;
using JustSaying.AwsTools.MessageHandling;
using JustSaying.Messaging.MessageSerialization;
using Microsoft.Extensions.Logging;
using NSubstitute;
using Shouldly;
using Xunit;

namespace JustSaying.UnitTests.AwsTools.MessageHandling.Sns.TopicByName
{
    public class WhenApplyingTags : WhenPublishingTestBase
    {
        private const string TopicArn = "topicarn";

        private Dictionary<string, string> _tags;
        private TagResourceRequest _actualCreateRequest;

        protected override Task<SnsTopicByName> CreateSystemUnderTestAsync()
        {
            var topicByName = new SnsTopicByName("TopicName", Sns, Substitute.For<IMessageSerializationRegister>(), Substitute.For<ILoggerFactory>(), new NonGenericMessageSubjectProvider())
            {
                Tags = _tags
            };

            return Task.FromResult(topicByName);
        }

        protected override void Given()
        {
            _tags = new Dictionary<string, string>
            {
                {
                    "TagOne", "Tag-One"
                },
                {
                    "TagTwo", "Tag-Two"
                }
            };

            Sns.FindTopicAsync(Arg.Any<string>())
                .Returns(new Topic { TopicArn = TopicArn });

            Sns.When(x => x.TagResourceAsync(Arg.Any<TagResourceRequest>()))
                .Do(x => _actualCreateRequest = x.Arg<TagResourceRequest>());
        }

        protected override async Task WhenAsync()
        {
            await SystemUnderTest.ExistsAsync();
            await SystemUnderTest.ApplyTagsAsync();
        }

        [Fact]
        public void TagResourceRequestIsIssued()
        {
            Sns.Received(1).TagResourceAsync(Arg.Any<TagResourceRequest>());
        }

        [Fact]
        public void TheCorrectTopicArnIsUsed()
        {
            _actualCreateRequest.ResourceArn.ShouldBe(TopicArn);
        }

        [Fact]
        public void TheTagsAreBuiltCorrectly()
        {
            foreach (var (key, value) in _tags)
            {
                _actualCreateRequest.Tags.ShouldContain(tag => tag.Key == key && tag.Value == value);
            }
        }
    }
}
