using System.Threading.Tasks;
using JustBehave;
using JustSaying.Messaging;
using JustSaying.Messaging.Monitoring;
using JustSaying.TestingFramework;
using NSubstitute;
using Shouldly;
using Xunit;

namespace JustSaying.UnitTests.JustSayingBus
{
    public class WhenPublishingMessageWithoutMonitor : GivenAServiceBusWithoutMonitoring
    {
        private readonly IMessagePublisher _publisher = Substitute.For<IMessagePublisher>();
        
        protected override async Task When()
        {
            SystemUnderTest.AddMessagePublisher<GenericMessage>(_publisher, string.Empty);
            await SystemUnderTest.PublishAsync(new GenericMessage());
        }

        [Fact]
        public void ANullMonitorIsProvidedByDefault()
        {
            SystemUnderTest.Monitor.ShouldBeAssignableTo<NullOpMessageMonitor>();
        }

        [Fact]
        public void SettingANullMonitorSetsTheMonitorToNullOpMonitor()
        {
            SystemUnderTest.Monitor = null;
            SystemUnderTest.Monitor.ShouldBeAssignableTo<NullOpMessageMonitor>();
        }

        [Fact]
        public void SettingANewMonitorIsAccepted()
        {
            SystemUnderTest.Monitor = new CustomMonitor();
            SystemUnderTest.Monitor.ShouldBeAssignableTo<CustomMonitor>();
        }
    }
}
