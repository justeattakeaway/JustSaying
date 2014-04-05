using System;
using NUnit.Framework;

namespace JustSaying.UnitTests.CreateMe
{
    public class WhenCreatingABus
    {
        private Action<IPublishConfiguration> _config;

        [TestFixtureSetUp]
        public void Given()
        {
            _config = x => { x.Region = "region-1"; };
        }

        [Test]
        public void StandardConfigurationIsRequired()
        {
            JustSaying.CreateMe.ABus(_config);
        }

        [Test]
        public void ThenICanProvideMonitoring()
        {
            JustSaying.CreateMe.ABus(_config).WithMonitoring(null);
        }

        [Test]
        public void MonitoringIsNotEnforced()
        {
            // Enforced by the fact we can do other configurations on the bus.
            JustSaying.CreateMe.ABus(_config).StopListening();
        }
    }
}
