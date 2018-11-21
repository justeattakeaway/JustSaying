using System;
using System.Threading.Tasks;
using JustSaying.Messaging;
using JustSaying.TestingFramework;
using Shouldly;
using Xunit;

namespace JustSaying.IntegrationTests.WhenRegisteringAPublisher
{
    [Collection(GlobalSetup.CollectionName)]
    public class WhenRegisteringAPublisherAndNotInstantiated : FluentNotificationStackTestBase
    {
        protected override async Task Given()
        {
            await base.Given();

            Configuration = new MessagingConfig();

            RecordAnyExceptionsThrown();
        }

        protected override async Task When()
        {
            await SystemUnderTest.PublishAsync(new SimpleMessage());
        }

        [AwsFact]
        public void ExceptionIsRaised()
        {
            ThrownException.ShouldBeAssignableTo<InvalidOperationException>();
        }
    }
}
