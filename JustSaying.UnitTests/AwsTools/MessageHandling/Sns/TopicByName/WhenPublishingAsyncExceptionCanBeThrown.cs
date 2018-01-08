using System;
using System.Net;
using System.Threading.Tasks;
using Amazon.SimpleNotificationService;
using Amazon.SimpleNotificationService.Model;
using JustBehave;
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
    public class WhenPublishingAsyncExceptionCanBeThrown : XAsyncBehaviourTest<SnsTopicByName>
    {
        private readonly IMessageSerialisationRegister _serialisationRegister = Substitute.For<IMessageSerialisationRegister>();
        private readonly IAmazonSimpleNotificationService _sns = Substitute.For<IAmazonSimpleNotificationService>();
        private const string TopicArn = "topicarn";

        protected override SnsTopicByName CreateSystemUnderTest()
        {
            var topic = new SnsTopicByName("TopicName", _sns, _serialisationRegister, Substitute.For<ILoggerFactory>(), new SnsWriteConfiguration
            {
                OnException = (ex, request) => false
            });

            topic.Exists();
            return topic;
        }

        protected override void Given()
        {
            _sns.FindTopicAsync("TopicName")
                .Returns(new Topic { TopicArn = TopicArn });
        }

        protected override Task When()
        {
            _sns.PublishAsync(Arg.Any<PublishRequest>()).Returns(ThrowsException);
            return Task.CompletedTask;
        }

        [Fact]
        public async Task ExceptionIsThrown()
        {
            await Should.ThrowAsync<PublishException>(() => SystemUnderTest.PublishAsync(new GenericMessage()));
        }

        [Fact]
        public async Task ExceptionContainsContext()
        {
            try
            {
                await SystemUnderTest.PublishAsync(new GenericMessage());
            }
            catch (Exception e)
            {
                var exception = (WebException) e.InnerException;
                exception.Status.ShouldBe(WebExceptionStatus.Timeout);
            }
        }

        private static Task<PublishResponse> ThrowsException(CallInfo callInfo)
        {
            throw new WebException("Operation timed out", WebExceptionStatus.Timeout);
        }
    }
}
