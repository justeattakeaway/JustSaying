using System;
using System.Threading;
using JustEat.Simples.NotificationStack.AwsTools;
using JustEat.Testing;
using NUnit.Framework;
using SimpleMessageMule.TestingFramework;

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
        }

        [Then]
        public void NoExceptionIsThrown()
        {
            Assert.That(ThrownException, Is.Null);
        }

        [Then]
        public void CustomExceptionHandlingIsCalled()
        {
            Patiently.AssertThat(() => _handledException);
        }

        protected override void Given()
        {
        }
    }
}