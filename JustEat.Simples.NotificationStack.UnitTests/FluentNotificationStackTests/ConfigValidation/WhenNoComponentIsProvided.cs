using System;
using JustEat.Simples.NotificationStack.Stack;
using JustEat.Testing;
using NUnit.Framework;

namespace Stack.UnitTests.FluentNotificationStackTests.ConfigValidation
{
    public class WhenNoComponentIsProvided : BaseConfigValidationTest
    {
        protected override void When()
        {
            FluentNotificationStack.Register(
                configuration =>
                    {
                        configuration.Environment = "x";
                        configuration.Tenant = "y";
                    });
        }

        [Then]
        public void ConfigItemsAreRequired()
        {
            Assert.IsInstanceOf<ArgumentNullException>(ThrownException);
        }

        [Then]
        public void ComponentIsRequested()
        {
            Assert.AreEqual(((ArgumentException)ThrownException).ParamName, "config.Component");
        }
    }
}