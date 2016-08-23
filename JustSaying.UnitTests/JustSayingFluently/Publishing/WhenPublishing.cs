using System.Threading.Tasks;
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

        protected override async Task When()
        {
            await SystemUnderTest.Publish(_message);
        }

        [Then]
        public void TheMessageIsPublished()
        {
            Bus.Received().Publish(_message);
        }
    }
}