using System;
using Amazon.DynamoDBv2.DataModel;
using Amazon.SimpleNotificationService;
using Amazon.SimpleNotificationService.Model;
using JustEat.Simples.NotificationStack.AwsTools;
using JustEat.Simples.NotificationStack.Messaging.MessageSerialisation;
using JustEat.Simples.NotificationStack.Messaging.Messages;
using JustEat.Simples.NotificationStack.Messaging.Messages.OrderDispatch;
using JustEat.Testing;
using NSubstitute;
using NUnit.Framework;

namespace AwsTools.UnitTests.Sns.TopicByArn
{
    public class WhenPublishing : BehaviourTest<SnsTopicByArn>
    {
        private readonly string message = "the_message_in_json";
        private IMessageSerialisationRegister _serialisationRegister = Substitute.For<IMessageSerialisationRegister>();
        private AmazonSimpleNotificationService _sns = Substitute.For<AmazonSimpleNotificationService>();
        private const string Arn = "arn";

        protected override SnsTopicByArn CreateSystemUnderTest()
        {
            return new SnsTopicByArn(Arn, _sns, _serialisationRegister);
        }

        protected override void Given()
        {
            var serialiser = Substitute.For<IMessageSerialiser<OrderAccepted>>();
            serialiser.Serialise(Arg.Any<OrderAccepted>()).Returns(message);
            _serialisationRegister.GetSerialiser(typeof (OrderAccepted)).Returns(serialiser);
        }

        protected override void When()
        {
            SystemUnderTest.Publish(new OrderAccepted(0, 0, 0));
        }

        [Then]
        public void MessageIsPublishedToSnsTopic()
        {
            _sns.Received().Publish(Arg.Is<PublishRequest>(x => x.Message == message));
        }

        [Then]
        public void MessageSubjectIsObjectType()
        {
            _sns.Received().Publish(Arg.Is<PublishRequest>(x => x.Subject == typeof(OrderAccepted).Name));
        }

        [Then]
        public void MessageIsPublishedToCorrectLocation()
        {
            _sns.Received().Publish(Arg.Is<PublishRequest>(x => x.TopicArn == Arn));
        }
    }
}
