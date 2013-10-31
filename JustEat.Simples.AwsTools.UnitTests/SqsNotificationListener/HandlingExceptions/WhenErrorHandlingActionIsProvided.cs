using System;
using System.Threading;
using JustEat.Simples.NotificationStack.AwsTools;
using JustEat.Testing;
using NUnit.Framework;

namespace AwsTools.UnitTests.SqsNotificationListener.HandlingExceptions
{
    public class WhenErrorHandlingActionIsProvided : BaseQueuePollingTest
    {
        private Action<Exception> _globalErrorHandler;
        private bool _handledException;

        protected override void When()
        {
            _globalErrorHandler = ex => { _handledException = true; };

            var listener = new JustEat.Simples.NotificationStack.AwsTools.SqsNotificationListener(null, null, new NullMessageFootprintStore(), null,
                                                       _globalErrorHandler);

            listener.HandleMessage(null);
            Thread.Sleep(200);
        }

        [Then]
        public void NoExceptionIsThrown()
        {
            Assert.That(ThrownException, Is.Null);
        }

        [Then]
        public void CustomExceptionHandlingIsCalled()
        {
            Assert.That(_handledException, Is.EqualTo(true));
        }

        protected override void Given()
        {
        }
    }
}