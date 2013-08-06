using System.Configuration;
using JustEat.Simples.Common.Services;
using JustEat.Testing;
using NUnit.Framework;

namespace JustEat.Simples.Common.UnitTests.Services.Configuration
{
    public class WhenGettingValueAndNotInConfigurationFile : BehaviourTest<ConfigurationService>
    {
        protected override void Given()
        {
            RecordAnyExceptionsThrown();
        }

        protected override void When()
        {
            SystemUnderTest.GetString("services.configuration.when.getting.value.and.not.in.configurationfile");
        }

        [Then]
        public void ExpectedExceptionIsThrown()
        {
            Assert.IsInstanceOf<ConfigurationErrorsException>(ThrownException);
        }
    }
}
