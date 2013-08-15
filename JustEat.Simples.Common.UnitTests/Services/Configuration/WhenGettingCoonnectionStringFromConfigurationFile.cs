using System.Configuration;
using JustEat.Simples.Common.Services;
using JustEat.Testing;
using NUnit.Framework;

namespace JustEat.Simples.Common.UnitTests.Services.Configuration
{
    public class WhenGettingCoonnectionStringFromConfigurationFile : BehaviourTest<ConfigurationService>
    {
        private string _actualValue;
        private const string ExpectedValue = "server=dummyserver";
        private const string ExpectedKey = "services.configuration.when.getting.connectionstring.from.configurationfile";

        protected override void Given()
        {
            Assert.AreEqual(
                ExpectedValue,
                ConfigurationManager.ConnectionStrings[ExpectedKey].ConnectionString,
                "The required connectionstring appears to be missing or incorrect.");
        }

        protected override void When()
        {
            _actualValue = SystemUnderTest.GetConnectionString(ExpectedKey);
        }

        [Then]
        public void ResultIsExpectedValue()
        {
            Assert.AreEqual(ExpectedValue, _actualValue);
        }
    }
}