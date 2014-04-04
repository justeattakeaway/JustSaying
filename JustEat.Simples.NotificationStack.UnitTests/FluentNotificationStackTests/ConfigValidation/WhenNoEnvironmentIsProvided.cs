using System;
using JustSaying.Messaging;
using JustSaying.Stack;
using JustEat.Testing;
using NUnit.Framework;

namespace Stack.UnitTests.FluentNotificationStackTests.ConfigValidation
{
    public class WhenNoEnvironmentIsProvided : BaseConfigValidationTest
    {
        protected override void When()
        {
            FluentNotificationStack.Register(
                configuration =>
                {
                    configuration.Region = "DefaultRegion";
                    configuration.Tenant = "x";
                });
        }

        [Then]
        public void ConfigItemsAreRequired()
        {
            Assert.IsInstanceOf<ArgumentException>(ThrownException);
        }

        [Then]
        public void EnvironmentIsRequested()
        {
            Assert.AreEqual(((ArgumentException)ThrownException).ParamName, "config.Environment");
        }
    }
}