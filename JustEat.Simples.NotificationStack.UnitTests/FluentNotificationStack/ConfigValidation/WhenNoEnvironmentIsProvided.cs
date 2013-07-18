using System;
using JustEat.Testing;
using NSubstitute;
using NUnit.Framework;

namespace Stack.UnitTests.FluentNotificationStack.ConfigValidation
{
    public class WhenNoEnvironmentIsProvided : BaseConfigValidationTest
    {
        protected override void Given()
        {
            Config.Tenant.Returns("x");
            base.Given();
        }

        [Then]
        public void ConfigItemsAreRequired()
        {
            Assert.IsInstanceOf<InvalidOperationException>(ThrownException);
        }
    }
}