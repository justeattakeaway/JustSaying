using JustEat.Simples.NotificationStack.Stack;
using JustEat.Testing;
using NUnit.Framework;

namespace Stack.UnitTests.FluentNotificationStackTests.ConfigValidation
{
    public class WhenErrorHandlingActionIsNotProvided : BaseConfigValidationTest
    {
        protected override void When()
        {
            FluentNotificationStack.Register(
                configuration => { configuration.Environment = "x"; configuration.Tenant = "y"; configuration.Component = "z"; }, 
                onError: null /* Null value under test */);
        }

        [Then]
        public void NoExceptionIsThrown()
        {
            Assert.That(ThrownException, Is.Null);
        }

        [Then]
        public void DefaultGlobalErrorHandlerIsProvided()
        {
            Assert.That(FluentNotificationStack.GlobalErrorHandler, Is.Not.Null);
        }
    }
}