using System;
using System.Threading.Tasks;
using Amazon.SimpleNotificationService.Model;
using JustSaying.Messaging;
using JustSaying.AwsTools.MessageHandling;
using JustSaying.Messaging.MessageSerialization;
using JustSaying.Models;
using JustSaying.TestingFramework;
using Microsoft.Extensions.Logging;
using NSubstitute;
using Xunit;

namespace JustSaying.UnitTests.AwsTools.MessageHandling.Sns.TopicByName
{
    public class WhenPublishing : WhenPublishingTestBase
    {
        private const string Message = "the_message_in_json";
        private readonly IMessageSerializationRegister _serializationRegister = Substitute.For<IMessageSerializationRegister>();
        private const string TopicArn = "topicarn";

        private protected override async Task<SnsTopicByName> CreateSystemUnderTestAsync()
        {
            var topic = new SnsTopicByName("TopicName", Sns, _serializationRegister, Substitute.For<ILoggerFactory>(), new NonGenericMessageSubjectProvider());
            await topic.ExistsAsync();
            return topic;
        }

        protected override void Given()
        {
            _serializationRegister.Serialize(Arg.Any<Message>(), Arg.Is(true)).Returns(Message);
            Sns.FindTopicAsync("TopicName")
                .Returns(new Topic { TopicArn = TopicArn });
        }

        protected override async Task WhenAsync()
        {
            await SystemUnderTest.PublishAsync(new SimpleMessage());
        }

        [Fact]
        public void MessageIsPublishedToSnsTopic()
        {
            Sns.Received().PublishAsync(Arg.Is<PublishRequest>(x => B(x)));
        }

        private static bool B(PublishRequest x)
        {
            return x.Message.Equals(Message, StringComparison.Ordinal);
        }

        [Fact]
        public void MessageSubjectIsObjectType()
        {
            Sns.Received().PublishAsync(Arg.Is<PublishRequest>(x => x.Subject == typeof(SimpleMessage).Name));
        }

        [Fact]
        public void MessageIsPublishedToCorrectLocation()
        {
            Sns.Received().PublishAsync(Arg.Is<PublishRequest>(x => x.TopicArn == TopicArn));
        }
    }
}
