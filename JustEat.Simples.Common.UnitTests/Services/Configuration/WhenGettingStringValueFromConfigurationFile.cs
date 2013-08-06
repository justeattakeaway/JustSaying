using System.Configuration;
using JustEat.Simples.Common.Services;
using JustEat.Testing;
using NUnit.Framework;

namespace JustEat.Simples.Common.UnitTests.Services.Configuration
{
    public class WhenGettingStringFromConfigurationFile : BehaviourTest<ConfigurationService>
    {
        private string _actualValue;
        private const string ExpectedValue = "StringValue";
        private const string ExpectedKey = "services.configuration.when.getting.string.from.configurationfile";

        protected override void Given()
        {
            Assert.AreEqual(
                ExpectedValue, 
                ConfigurationManager.AppSettings[ExpectedKey], 
                "The required appsetting appears to be missing or incorrect.");
        }

        protected override void When()
        {
            _actualValue = SystemUnderTest.GetString(ExpectedKey);
        }

        [Then]
        public void ResultIsExpectedValue()
        {
            Assert.AreEqual(ExpectedValue, _actualValue);
        }
    }
}