using JustBehave;
using JustSaying.AwsTools;
using JustSaying.Messaging;
using JustSaying.TestingFramework;
using NSubstitute;
using NUnit.Framework;

namespace JustSaying.UnitTests.JustSayingBus
{
    public class WhenAddingAPublisherWithNoTopic : GivenAServiceBus
    {
        protected override void Given()
        {
            RecordAnyExceptionsThrown();
        }

        protected override void When()
        {
            SystemUnderTest.AddMessagePublisher<GenericMessage>(Substitute.For<IPublisher>(), string.Empty);
        }

        [Then]
        public void ExceptionThrown()
        {
            Assert.That(ThrownException, Is.Not.Null);
        }
    }
}