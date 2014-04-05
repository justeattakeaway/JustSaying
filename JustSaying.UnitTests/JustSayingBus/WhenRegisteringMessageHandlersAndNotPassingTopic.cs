using System;
using JustEat.Testing;
using JustSaying.Messaging;
using JustSaying.Messaging.MessageHandling;
using JustSaying.Messaging.Messages;
using NSubstitute;
using NUnit.Framework;

namespace JustSaying.UnitTests.JustSayingBus
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