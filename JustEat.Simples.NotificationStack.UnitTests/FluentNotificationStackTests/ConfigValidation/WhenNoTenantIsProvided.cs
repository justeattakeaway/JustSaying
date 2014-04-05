using System;
using JustSaying.Messaging;
using JustSaying.Stack;
using JustEat.Testing;
using NUnit.Framework;

namespace Stack.UnitTests.FluentNotificationStackTests.ConfigValidation
{
    public class WhenNoTenantIsProvided : BaseConfigValidationTest
    {
        protected override void When()
        {
            FluentNotificationStack.Register(
                configuration =>
                {
                    configuration.Region = "DefaultRegion";
                    configuration.Environment = "x";
                });
        }

        [Then]
        public void ConfigItemsAreRequired()
        {
            Assert.IsInstanceOf<ArgumentNullException>(ThrownException);
        }

        [Then]
        public void TenantIsRequested()
        {
            Assert.AreEqual(((ArgumentException)ThrownException).ParamName, "config.Tenant");
        }
    }
}
