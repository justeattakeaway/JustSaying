using System.Threading;
using System.Threading.Tasks;
using JustSaying.Messaging;
using JustSaying.Messaging.Monitoring;
using JustSaying.TestingFramework;
using Microsoft.Extensions.Logging.Abstractions;
using NSubstitute;
using Shouldly;
using Xunit;

namespace JustSaying.UnitTests.JustSayingBus
{
    public class WhenPublishingMessageWithoutMonitor : GivenAServiceBusWithoutMonitoring
    {
        private readonly IMessagePublisher _publisher = Substitute.For<IMessagePublisher>();

        protected override async Task WhenAsync()
        {
            SystemUnderTest.AddMessagePublisher<SimpleMessage>(_publisher);
            await SystemUnderTest.StartAsync(CancellationToken.None);
            await SystemUnderTest.PublishAsync(new SimpleMessage());
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
            SystemUnderTest.Monitor = new TrackingLoggingMonitor(NullLogger.Instance);
            SystemUnderTest.Monitor.ShouldBeAssignableTo<TrackingLoggingMonitor>();
        }
    }
}
