using System;
using System.Threading;
using JustEat.Simples.NotificationStack.AwsTools;
using JustEat.Testing;
using NUnit.Framework;
using Stack.UnitTests.FluentNotificationStackTests.ConfigValidation;

namespace Stack.UnitTests.NotificationStack
{
    public class WhenHandlingMessageThatThrowsExceptions : BaseConfigValidationTest
    {
        private Action<Exception> _globalErrorHandler;
        private bool _wasCalled;

        protected override void When()
        {
            _globalErrorHandler = ex => { _wasCalled = true; };

            var listener = new SqsNotificationListener(null, null, new NullMessageFootprintStore(), null, _globalErrorHandler);
            
            listener.HandleMessage(null);
        }

        [Then]
        public void GlobalErrorHandlerIsSameAsErrorHandlerProvided()
        {
            // This is a hack to work around the fact that the inner
            // code is spinning up in a task, without returning any kind 
            // of continuation or awaitable thing.
            Thread.Sleep(200);

            Assert.That(_wasCalled, Is.True);
        }
    }
}