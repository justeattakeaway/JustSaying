using JustBehave;
using JustSaying.Models;
using JustSaying.TestingFramework;
using NSubstitute;

namespace JustSaying.UnitTests.JustSayingFluently.Publishing
{
    public class WhenPublishing : JustSayingFluentlyTestBase
    {
        private readonly Message _message = new GenericMessage();

        protected override void Given(){}

        protected override void When()
        {
            SystemUnderTest.PublishAsync(_message)
                .GetAwaiter().GetResult();
        }

        [Then]
        public void TheMessageIsPublished()
        {
            Bus.Received().PublishAsync(_message);
        }
    }
}
