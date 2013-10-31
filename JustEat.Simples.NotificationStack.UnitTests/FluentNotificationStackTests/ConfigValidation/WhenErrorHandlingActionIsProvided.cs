using System;
using JustEat.Simples.NotificationStack.Stack;
using JustEat.Testing;
using NUnit.Framework;

namespace Stack.UnitTests.FluentNotificationStackTests.ConfigValidation
{
    public class WhenErrorHandlingActionIsProvided : BaseConfigValidationTest
    {
        private Action<Exception> _globalErrorHandler;

        protected override void When()
        {
            _globalErrorHandler = ex => { };

            FluentNotificationStack.Register(
                configuration => { configuration.Environment = "x"; configuration.Tenant = "y"; configuration.Component = "z"; }, 
                _globalErrorHandler);
        }

        [Then]
        public void NoExceptionIsThrown()
        {
            Assert.That(ThrownException, Is.Null);
        }

        [Then]
        public void GlobalErrorHandlerIsSameAsErrorHandlerProvided()
        {
            Assert.That(FluentNotificationStack.GlobalErrorHandler, Is.EqualTo(_globalErrorHandler));
        }
    }
}
