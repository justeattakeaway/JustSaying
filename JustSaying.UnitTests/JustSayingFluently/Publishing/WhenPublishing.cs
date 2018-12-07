using System.Threading;
using System.Threading.Tasks;
using JustSaying.Messaging;
using JustSaying.TestingFramework;
using NSubstitute;
using Xunit;

namespace JustSaying.UnitTests.JustSayingFluently.Publishing
{
    public class WhenPublishing : JustSayingFluentlyTestBase
    {
        private readonly SimpleMessage _message = new SimpleMessage();

        protected override Task Given() => Task.CompletedTask;

        protected override async Task When()
        {
            await SystemUnderTest.PublishAsync(_message);
        }

        [Fact]
        public void TheMessageIsPublished()
        {
            Received.InOrder(async () => await Bus.PublishAsync(_message, null, CancellationToken.None));
        }
    }
}
