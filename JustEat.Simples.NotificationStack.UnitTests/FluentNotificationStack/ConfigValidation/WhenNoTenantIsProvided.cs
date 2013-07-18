using System;
using JustEat.Testing;
using NSubstitute;
using NUnit.Framework;

namespace Stack.UnitTests.FluentNotificationStack.ConfigValidation
{
    public class WhenNoTenantIsProvided : BaseConfigValidationTest
    {
        protected override void Given()
        {
            Config.Environment.Returns("x");
            base.Given();
        }

        [Then]
        public void ConfigItemsAreRequired()
        {
            Assert.IsInstanceOf<InvalidOperationException>(ThrownException);
        }
    }
}
