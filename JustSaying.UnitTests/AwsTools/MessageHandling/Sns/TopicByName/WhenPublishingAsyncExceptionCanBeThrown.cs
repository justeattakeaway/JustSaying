using System;
using System.Net;
using System.Threading.Tasks;
using Amazon.SimpleNotificationService.Model;
using JustSaying.Messaging;
using JustSaying.AwsTools.MessageHandling;
using JustSaying.AwsTools.QueueCreation;
using JustSaying.Messaging.MessageSerialization;
using JustSaying.TestingFramework;
using Microsoft.Extensions.Logging;
using NSubstitute;
using NSubstitute.Core;
using Shouldly;
using Xunit;

namespace JustSaying.UnitTests.AwsTools.MessageHandling.Sns.TopicByName
{
    public class WhenPublishingAsyncExceptionCanBeThrown : WhenPublishingTestBase
    {
        private readonly IMessageSerializationRegister _serializationRegister = Substitute.For<IMessageSerializationRegister>();
        private const string TopicArn = "topicarn";

        protected override async Task<SnsTopicByName> CreateSystemUnderTestAsync()
        {
            var topic = new SnsTopicByName("TopicName", Sns, _serializationRegister, Substitute.For<ILoggerFactory>(), new SnsWriteConfiguration
            {
                HandleException = (ex, m) => false
            }, Substitute.For<IMessageSubjectProvider>());

            await topic.ExistsAsync();
            return topic;
        }

        protected override void Given()
        {
            Sns.FindTopicAsync("TopicName")
                .Returns(new Topic { TopicArn = TopicArn });
        }

        protected override Task WhenAction()
        {
            Sns.PublishAsync(Arg.Any<PublishRequest>()).Returns(ThrowsException);
            return Task.CompletedTask;
        }

        [Fact]
        public async Task ExceptionIsThrown()
        {
            await Should.ThrowAsync<PublishException>(() => SystemUnderTest.PublishAsync(new SimpleMessage()));
        }

        [Fact]
        public async Task ExceptionContainsContext()
        {
            try
            {
                await SystemUnderTest.PublishAsync(new SimpleMessage());
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
