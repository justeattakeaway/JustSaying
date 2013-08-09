using System;
using JustEat.Simples.NotificationStack.Messaging;
using JustEat.Simples.NotificationStack.Stack;
using JustEat.Testing;
using NUnit.Framework;

namespace Stack.UnitTests.FluentNotificationStackTests.ConfigValidation
{
    public class WhenNoTenantIsProvided : BaseConfigValidationTest
    {
        protected override void When()
        {
            FluentNotificationStack.Register(
                Component.OrderEngine,
                configuration =>
                {
                    configuration.Environment = "x";
                });
        }

        [Then]
        public void ConfigItemsAreRequired()
        {
            Assert.IsInstanceOf<InvalidOperationException>(ThrownException);
        }
    }
}
