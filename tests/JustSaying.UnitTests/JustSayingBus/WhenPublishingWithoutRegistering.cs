using System;
using System.Threading.Tasks;
using JustSaying.Messaging;
using JustSaying.Models;
using NSubstitute;
using Shouldly;
using Xunit;
using Xunit.Abstractions;

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

        public WhenPublishingWithoutRegistering(ITestOutputHelper outputHelper) : base(outputHelper)
        { }
    }
}
