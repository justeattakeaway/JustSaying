using System;
using System.Threading.Tasks;
using JustSaying.Messaging;
using JustSaying.TestingFramework;
using NSubstitute;
using Shouldly;
using Xunit;

namespace JustSaying.UnitTests.JustSayingBus
{
    public class WhenPublishingWithoutRegistering : GivenAServiceBus
    {
        protected override void Given()
        {
            base.Given();
            RecordAnyExceptionsThrown();
        }

        protected override async Task WhenAsync()
        {
            await SystemUnderTest.PublishAsync(Substitute.For<Message>());
        }

        [Fact]
        public void InvalidOperationIsThrown()
        {
            ThrownException.ShouldBeAssignableTo<InvalidOperationException>();
        }
    }
}
