using System;
using System.Threading.Tasks;
using Amazon.SimpleNotificationService;
using Amazon.SimpleNotificationService.Model;
using JustBehave;
using JustSaying.Messaging;
using JustSaying.AwsTools.MessageHandling;
using JustSaying.Messaging.MessageSerialization;
using JustSaying.Models;
using Microsoft.Extensions.Logging;
using NSubstitute;
using Xunit;

namespace JustSaying.UnitTests.AwsTools.MessageHandling.Sns.TopicByName
{
    public class WhenPublishingAsyncWithGenericMessageSubjectProvider : XAsyncBehaviourTest<SnsTopicByName>
    {
        public class MessageWithTypeParameters<T, U> : Message
        {

        }

        private const string Message = "the_message_in_json";
        private readonly IMessageSerializationRegister _serializationRegister = Substitute.For<IMessageSerializationRegister>();
        private readonly IAmazonSimpleNotificationService _sns = Substitute.For<IAmazonSimpleNotificationService>();
        private const string TopicArn = "topicarn";

        protected override async Task<SnsTopicByName> CreateSystemUnderTestAsync()
        {
            var topic = new SnsTopicByName("TopicName", _sns, _serializationRegister, Substitute.For<ILoggerFactory>(), new GenericMessageSubjectProvider());
            await topic.ExistsAsync();
            return topic;
        }

        protected override Task Given()
        {
            _serializationRegister.Serialize(Arg.Any<Message>(), Arg.Is(true)).Returns(Message);
            _sns.FindTopicAsync("TopicName")
                .Returns(new Topic { TopicArn = TopicArn });

            return Task.CompletedTask;
        }

        protected override async Task When()
        {
            await SystemUnderTest.PublishAsync(new MessageWithTypeParameters<int, string>());
        }

        [Fact]
        public void MessageIsPublishedToSnsTopic()
        {
            _sns.Received().PublishAsync(Arg.Is<PublishRequest>(x => B(x)));
        }

        private static bool B(PublishRequest x)
        {
            return x.Message.Equals(Message, StringComparison.Ordinal);
        }

        [Fact]
        public void MessageSubjectIsObjectType()
        {
            _sns.Received().PublishAsync(Arg.Is<PublishRequest>(x => x.Subject == new GenericMessageSubjectProvider().GetSubjectForType(typeof(MessageWithTypeParameters<int, string>))));
        }

        [Fact]
        public void MessageIsPublishedToCorrectLocation()
        {
            _sns.Received().PublishAsync(Arg.Is<PublishRequest>(x => x.TopicArn == TopicArn));
        }
    }
}
