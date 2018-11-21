using System.Net;
using System.Threading.Tasks;
using Amazon.SimpleNotificationService;
using Amazon.SimpleNotificationService.Model;
using JustBehave;
using JustSaying.Messaging;
using JustSaying.AwsTools.MessageHandling;
using JustSaying.AwsTools.QueueCreation;
using JustSaying.Messaging.MessageSerialisation;
using JustSaying.TestingFramework;
using Microsoft.Extensions.Logging;
using NSubstitute;
using NSubstitute.Core;
using Shouldly;
using Xunit;

namespace JustSaying.UnitTests.AwsTools.MessageHandling.Sns.TopicByName
{
    public class WhenPublishingAsyncExceptionCanBeHandled : XAsyncBehaviourTest<SnsTopicByName>
    {
        private readonly IMessageSerialisationRegister _serialisationRegister = Substitute.For<IMessageSerialisationRegister>();
        private readonly IAmazonSimpleNotificationService _sns = Substitute.For<IAmazonSimpleNotificationService>();
        private const string TopicArn = "topicarn";

        protected override async Task<SnsTopicByName> CreateSystemUnderTestAsync()
        {
            var topic = new SnsTopicByName("TopicName", _sns, _serialisationRegister, Substitute.For<ILoggerFactory>(), new SnsWriteConfiguration
            {
                HandleException = (ex, m) => true
            }, Substitute.For<IMessageSubjectProvider>());

            await topic.ExistsAsync();
            return topic;
        }

        protected override Task Given()
        {
            _sns.FindTopicAsync("TopicName")
                .Returns(new Topic { TopicArn = TopicArn });
            return Task.CompletedTask;
        }

        protected override Task When()
        {
            _sns.PublishAsync(Arg.Any<PublishRequest>()).Returns(ThrowsException);
            return Task.CompletedTask;
        }

        [Fact]
        public async Task FailSilently()
        {
            var unexpectedException = await Record.ExceptionAsync(
                () => SystemUnderTest.PublishAsync(new SimpleMessage()));
            unexpectedException.ShouldBeNull();
        }

        private static Task<PublishResponse> ThrowsException(CallInfo callInfo)
        {
            throw new WebException("Operation timed out", WebExceptionStatus.Timeout);
        }
    }
}
