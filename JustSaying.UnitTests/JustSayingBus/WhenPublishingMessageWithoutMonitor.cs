using JustBehave;
using JustSaying.AwsTools;
using JustSaying.Messaging.Monitoring;
using JustSaying.TestingFramework;
using NSubstitute;
using NUnit.Framework;

namespace JustSaying.UnitTests.JustSayingBus
{
    public class WhenPublishingMessageWithoutMonitor : GivenAServiceBusWithoutMonitoring
    {
        private readonly IPublisher _publisher = Substitute.For<IPublisher>();
        
        protected override void When()
        {
            SystemUnderTest.AddMessagePublisher<GenericMessage>(_publisher, string.Empty);
            SystemUnderTest.Publish(new GenericMessage());
        }

        [Then]
        public void ANullMonitorIsProvidedByDefault()
        {
            Assert.IsInstanceOf<NullOpMessageMonitor>(SystemUnderTest.Monitor);
        }

        [Then]
        public void SettingANullMonitorSetsTheMonitorToNullOpMonitor()
        {
            SystemUnderTest.Monitor = null;
            Assert.IsInstanceOf<NullOpMessageMonitor>(SystemUnderTest.Monitor);
        }

        [Then]
        public void SettingANewMonitorIsAccepted()
        {
            SystemUnderTest.Monitor = new CustomMonitor();
            Assert.IsInstanceOf<CustomMonitor>(SystemUnderTest.Monitor);
        }
    }
}