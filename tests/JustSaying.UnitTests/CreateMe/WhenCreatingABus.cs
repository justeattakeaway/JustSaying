using System;
using Microsoft.Extensions.Logging;
using Xunit;

namespace JustSaying.UnitTests.CreateMe
{
    public class WhenCreatingABus
    {
        private readonly Action<IPublishConfiguration> _config;
        private readonly string _region;

        public WhenCreatingABus()
        {
            _region = "region-1";
            _config = x =>
            {
                x.PublishFailureBackoff = TimeSpan.FromMilliseconds(50);
                x.PublishFailureReAttempts = 2;
            };
        }

        [Fact]
        public void PublishConfigurationIsOptional()
        {
            // Enforced by the fact we can do other configurations on the bus.
            CreateMeABus.WithLogging(new LoggerFactory()).InRegion(_region);
        }

        [Fact]
        public void PublishConfigurationCanBeProvided()
        {
            CreateMeABus.WithLogging(new LoggerFactory()).InRegion(_region).ConfigurePublisherWith(_config);
        }

        [Fact]
        public void ThenICanProvideMonitoring()
        {
            CreateMeABus.WithLogging(new LoggerFactory()).InRegion(_region).WithMonitoring(null).ConfigurePublisherWith(_config);
        }

        [Fact]
        public void MonitoringIsNotEnforced()
        {
            // Enforced by the fact we can do other configurations on the bus.
            CreateMeABus.WithLogging(new LoggerFactory()).InRegion(_region).ConfigurePublisherWith(_config);
        }
    }
}
