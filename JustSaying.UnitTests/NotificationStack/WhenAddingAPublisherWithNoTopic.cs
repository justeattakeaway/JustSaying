using System;
using JustSaying.Messaging;
using JustEat.Testing;
using JustSaying.Tests.MessageStubs;
using NSubstitute;
using NUnit.Framework;

namespace JustSaying.UnitTests.NotificationStack
{
    public class WhenAddingAPublisherWithNoTopic : GivenAServiceBus
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
        public void ExceptionThrown()
        {
            Assert.That(ThrownException, Is.Not.Null);
        }
    }
}