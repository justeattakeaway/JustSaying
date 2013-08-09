using System.Configuration;
using JustEat.Simples.Common.Services;
using JustEat.Testing;
using NUnit.Framework;

namespace JustEat.Simples.Common.UnitTests.Services.Configuration
{
    public class WhenGettingBoolFromConfigurationFile : BehaviourTest<ConfigurationService>
    {
        private bool _actualValue;
        private const bool ExpectedValue = true;
        private const string ExpectedKey = "services.configuration.when.getting.bool.from.configurationfile";

        protected override void Given()
        {
            Assert.AreEqual(
                ExpectedValue,
                bool.Parse(ConfigurationManager.AppSettings[ExpectedKey]),
                "The required appsetting appears to be missing or incorrect.");
        }

        protected override void When()
        {
            _actualValue = SystemUnderTest.GetBoolean(ExpectedKey);
        }

        [Then]
        public void ResultIsExpectedValue()
        {
            Assert.AreEqual(ExpectedValue, _actualValue);
        }
    }
}