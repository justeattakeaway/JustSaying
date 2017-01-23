using System;
using NUnit.Framework;
using Microsoft.Extensions.Logging;

namespace JustSaying.UnitTests.CreateMe
{
    public class WhenCreatingABus
    {
        private Action<IPublishConfiguration> _config;
        private string _region;

        [OneTimeSetUp]
        public void Given()
        {
            _region = "region-1";
            _config = x =>
            {
                x.PublishFailureBackoffMilliseconds = 50;
                x.PublishFailureReAttempts = 2;
            };
        }

        [Test]
        public void PublishConfigurationIsOptional()
        {
            // Enforced by the fact we can do other configurations on the bus.
            CreateMeABus.WithLogging(new LoggerFactory()).InRegion(_region).StopListening();
        }

        [Test]
        public void PublishConfigurationCanBeProvided()
        {
            CreateMeABus.WithLogging(new LoggerFactory()).InRegion(_region).ConfigurePublisherWith(_config);
        }

        [Test]
        public void ThenICanProvideMonitoring()
        {
            CreateMeABus.WithLogging(new LoggerFactory()).InRegion(_region).WithMonitoring(null).ConfigurePublisherWith(_config);
        }

        [Test]
        public void MonitoringIsNotEnforced()
        {
            // Enforced by the fact we can do other configurations on the bus.
            CreateMeABus.WithLogging(new LoggerFactory()).InRegion(_region).ConfigurePublisherWith(_config).StopListening();
        }

        [Test]
        public void ThenICanProvideCustomSerialisation()
        {
            CreateMeABus.WithLogging(new LoggerFactory()).InRegion(_region).WithSerialisationFactory(null);
        }

        [Test]
        public void CustomSerialisationIsNotEnforced()
        {
            // Enforced by the fact we can do other configurations on the bus.
            CreateMeABus.WithLogging(new LoggerFactory()).InRegion(_region).WithSerialisationFactory(null).StopListening();
        }
    }
}
