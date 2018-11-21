using System;
using System.Threading.Tasks;
using JustSaying.Models;
using NSubstitute;
using Shouldly;
using Xunit;

namespace JustSaying.UnitTests.JustSayingBus
{
    public class WhenPublishingWithoutRegistering : GivenAServiceBus
    {
        protected override async Task Given()
        {
            await base.Given();
            RecordAnyExceptionsThrown();
        }

        protected override async Task When()
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
