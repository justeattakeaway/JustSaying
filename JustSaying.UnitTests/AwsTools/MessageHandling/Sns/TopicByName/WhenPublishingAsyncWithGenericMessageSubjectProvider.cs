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
    public class WhenPublishingAsyncWithGenericMessageSubjectProvider : XAsyncBehaviourTest<SnsTopicByName>
    {
        public class MessageWithTypeParameters<T, U> : Message
        {

        }

        private const string Message = "the_message_in_json";
        private readonly IMessageSerialisationRegister _serialisationRegister = Substitute.For<IMessageSerialisationRegister>();
        private readonly IAmazonSimpleNotificationService _sns = Substitute.For<IAmazonSimpleNotificationService>();
        private const string TopicArn = "topicarn";

        protected override SnsTopicByName CreateSystemUnderTest()
        {
            var topic = new SnsTopicByName("TopicName", _sns, _serialisationRegister, Substitute.For<ILoggerFactory>(), new GenericMessageSubjectProvider());
            topic.ExistsAsync().GetAwaiter().GetResult();;
            return topic;
        }

        protected override void Given()
        {
            _serialisationRegister.Serialise(Arg.Any<Message>(), Arg.Is(true)).Returns(Message);
            _sns.FindTopicAsync("TopicName")
                .Returns(new Topic { TopicArn = TopicArn });
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
            return x.Message.Equals(Message);
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
