using System;
using JustSaying.Messaging;
using JustEat.Testing;
using NSubstitute;
using NUnit.Framework;

namespace Stack.UnitTests.NotificationStack
{
    public class WhenRegisteringMultipleSubscribersToTheSameTopic : NotificationStackBaseTest
    {
        protected override void Given()
        {
            RecordAnyExceptionsThrown();
        }

        protected override void When()
        {
            SystemUnderTest.AddNotificationTopicSubscriber(NotificationTopic.OrderDispatch, Substitute.For<INotificationSubscriber>());
            SystemUnderTest.AddNotificationTopicSubscriber(NotificationTopic.OrderDispatch, Substitute.For<INotificationSubscriber>());
        }

        [Then]
        public void AnExceptionOccurs()
        {
            Assert.IsInstanceOf<ArgumentException>(ThrownException);
        }

        [Then]
        public void AHelpfulExceptionIsThrown()
        {
            Assert.That(ThrownException.Message.Contains("OrderDispatch"));
        }
    }
}