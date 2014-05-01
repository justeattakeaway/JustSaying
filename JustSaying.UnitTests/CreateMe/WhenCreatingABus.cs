using System;
using NUnit.Framework;

namespace JustSaying.UnitTests.CreateMe
{
    public class WhenCreatingABus
    {
        private Action<IPublishConfiguration> _config;
        private string _region;

        [TestFixtureSetUp]
        public void Given()
        {
            _region = "region-1";
            _config = x =>
            {
                x.Region = _region;
            };
        }

        [Test]
        public void StandardConfigurationIsRequired()
        {
            JustSaying.CreateMeABus.InRegion(_region).ConfigurePublisherWith(_config);
        }

        [Test]
        public void ThenICanProvideMonitoring()
        {
            JustSaying.CreateMeABus.InRegion(_region).ConfigurePublisherWith(_config).WithMonitoring(null);
        }

        [Test]
        public void MonitoringIsNotEnforced()
        {
            // Enforced by the fact we can do other configurations on the bus.
            JustSaying.CreateMeABus.InRegion(_region).ConfigurePublisherWith(_config).StopListening();
        }
    }
}
