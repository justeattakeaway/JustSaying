using System;
using JustSaying.Messaging;
using JustSaying.Messaging.MessageHandling;
using JustSaying.Messaging.Messages;
using JustEat.Testing;
using NSubstitute;
using NUnit.Framework;

namespace JustSaying.UnitTests.NotificationStack
{
    public class WhenRegisteringMessageHandlersAndNotPassingTopic : GivenAServiceBus
    {
        protected override void Given()
        {
            base.Given();
            RecordAnyExceptionsThrown();
        }

        protected override void When()
        {
            SystemUnderTest.AddNotificationTopicSubscriber("OrderDispatch", Substitute.For<INotificationSubscriber>());
            SystemUnderTest.AddMessageHandler(" ", Substitute.For<IHandler<Message>>());
        }

        [Then]
        public void ArgExceptionThrown()
        {
            Assert.AreEqual(((ArgumentException)ThrownException).ParamName, "topic");
        }
    }
}