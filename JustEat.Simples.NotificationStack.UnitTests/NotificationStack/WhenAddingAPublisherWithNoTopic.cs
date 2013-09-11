using System;
using JustEat.Simples.NotificationStack.Messaging;
using JustEat.Testing;
using NSubstitute;
using NUnit.Framework;
using Tests.MessageStubs;

namespace Stack.UnitTests.NotificationStack
{
    public class WhenAddingAPublisherWithNoTopic : NotificationStackBaseTest
    {
        protected override void Given()
        {
            RecordAnyExceptionsThrown();
        }

        protected override void When()
        {
            SystemUnderTest.AddMessagePublisher<GenericMessage>(" ", Substitute.For<IMessagePublisher>());
        }

        [Then]
        public void ArgExceptionThrown()
        {
            Assert.AreEqual(((ArgumentException)ThrownException).ParamName, "topic");
        }
    }
}