using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Amazon.SimpleNotificationService;
using Amazon.SimpleNotificationService.Model;
using JustBehave;
using JustSaying.AwsTools.MessageHandling;
using JustSaying.Messaging.MessageSerialisation;
using JustSaying.Models;
using JustSaying.TestingFramework;
using Microsoft.Extensions.Logging;
using NSubstitute;
using Xunit;

namespace JustSaying.UnitTests.AwsTools.MessageHandling.Sns.TopicByName
{
    public class WhenPublishingAsync : XAsyncBehaviourTest<SnsTopicByName>
    {
        private const string Message = "the_message_in_json";
        private const string MessageAttributeKey = "StringAttribute";
        private const string MessageAttributeValue = "StringValue";
        private const string MessageAttributeDataType = "String";
        private readonly IMessageSerialisationRegister _serialisationRegister = Substitute.For<IMessageSerialisationRegister>();
        private readonly IAmazonSimpleNotificationService _sns = Substitute.For<IAmazonSimpleNotificationService>();
        private const string TopicArn = "topicarn";

        protected override async Task<SnsTopicByName> CreateSystemUnderTestAsync()
        {
            var topic = new SnsTopicByName("TopicName", _sns, _serialisationRegister, Substitute.For<ILoggerFactory>(), new NonGenericMessageSubjectProvider());
            await topic.ExistsAsync();
            return topic;
        }

        protected override Task Given()
        {
            _serialisationRegister.Serialise(Arg.Any<Message>(), Arg.Is(true)).Returns(Message);

            _sns.FindTopicAsync("TopicName")
                .Returns(new Topic { TopicArn = TopicArn });
            return Task.CompletedTask;
        }

        protected override async Task When()
        {
            await SystemUnderTest.PublishAsync(new SimpleMessage
            {
                MessageAttributes = new Dictionary<string, Models.MessageAttributeValue>
                {
                    {
                        MessageAttributeKey,
                            new Models.MessageAttributeValue{
                                StringValue = MessageAttributeValue,
                                DataType = MessageAttributeDataType
                            }
                    }
                }
            });
        }

        [Fact]
        public void MessageIsPublishedToSnsTopic()
        {
            _sns.Received().PublishAsync(Arg.Is<PublishRequest>(x => B(x)));
        }

        private static bool B(PublishRequest x)
        {
            return x.Message.Equals(Message);
        }


        [Fact]
        public void MessageSubjectIsObjectType()
        {
            _sns.Received().PublishAsync(Arg.Is<PublishRequest>(x => x.Subject == typeof(SimpleMessage).Name));
        }

        [Fact]
        public void MessageIsPublishedToCorrectLocation()
        {
            _sns.Received().PublishAsync(Arg.Is<PublishRequest>(x => x.TopicArn == TopicArn));
        }

        [Fact]
        public void MessageAttributeKeyIsPublished()
        {
            _sns.Received().PublishAsync(Arg.Is<PublishRequest>(x => x.MessageAttributes.Single().Key == MessageAttributeKey));
        }

        [Fact]
        public void MessageAttributeValueIsPublished()
        {
            _sns.Received().PublishAsync(Arg.Is<PublishRequest>(x => x.MessageAttributes.Single().Value.StringValue == MessageAttributeValue));
        }

        [Fact]
        public void MessageAttributeDataTypeIsPublished()
        {
            _sns.Received().PublishAsync(Arg.Is<PublishRequest>(x => x.MessageAttributes.Single().Value.DataType == MessageAttributeDataType));
        }
    }
}
