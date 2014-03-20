using System;
using JustEat.Simples.NotificationStack.Messaging;
using JustEat.Simples.NotificationStack.Stack;
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