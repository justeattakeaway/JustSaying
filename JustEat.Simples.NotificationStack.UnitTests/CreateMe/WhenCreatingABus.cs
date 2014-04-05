using System;
using JustSaying;
using JustSaying.Stack;
using NUnit.Framework;

namespace Stack.UnitTests.CreateMe
{
    public class WhenCreatingABus
    {
        private Action<INotificationStackConfiguration> _config;

        [TestFixtureSetUp]
        public void Given()
        {
            _config = x =>
            {
                x.Region = "region-1";
                x.Environment = "unit-test";
                x.Component = "testing-component";
                x.Tenant = "some-country";
            };
        }

        [Test]
        public void ICanCreateAJustEatBusWithJustEatConfig()
        {
            JustSayingExtensions.CreateMe.AJustEatBus(_config);
        }
        
        [Test]
        public void ThenICanProvideMonitoring()
        {
            JustSayingExtensions.CreateMe.AJustEatBus(_config).WithMonitoring(null);
        }
    }
}
