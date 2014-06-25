using JustEat.Testing;
using JustSaying.Tests.MessageStubs;
using JustSaying.Models;
using NSubstitute;

namespace JustSaying.UnitTests.JustSayingFluently.Publishing
{
    public class WhenPublishing : JustSayingFluentlyTestBase
    {
        private readonly Message _message = new GenericMessage();

        protected override void Given(){}

        protected override void When()
        {
            SystemUnderTest.Publish(_message);
        }

        [Then]
        public void TheMessageIsPublished()
        {
            NotificationStack.Received().Publish(_message);
        }
    }
}