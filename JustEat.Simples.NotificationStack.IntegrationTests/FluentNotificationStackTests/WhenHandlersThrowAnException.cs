using System;
using System.Linq;
using System.Threading;
using Amazon;
using Amazon.SQS.Model;
using JustEat.Simples.NotificationStack.AwsTools;
using JustEat.Simples.NotificationStack.Stack;
using JustEat.Testing;
using NSubstitute;
using NUnit.Framework;
using Tests.MessageStubs;

namespace NotificationStack.IntegrationTests.FluentNotificationStackTests
{
    public class WhenHandlersThrowAnException : GivenANotificationStack
    {
        private readonly Future<GenericMessage> _handler = new Future<GenericMessage>(() => { throw new Exception(""); });
        private SqsQueueByName _queue;
        private string _component;

        protected override void Given()
        {
            _component = "test" + DateTime.Now.Ticks;
            RecordAnyExceptionsThrown();
            base.Given();
            RegisterHandler(_handler);
            RegisterConfig(new MessagingConfig { Component = _component, Tenant = "uk", Environment = "int", PublishFailureBackoffMilliseconds = 1, PublishFailureReAttempts = 3 });
        }

        protected override void When()
        {
            ServiceBus.Publish(new GenericMessage());
        }

        [Then, Explicit]
        public void ThenExceptionIsRecordedInStatsD()
        {
            _handler.WaitUntilCompletion(10.Seconds()).ShouldBeTrue();
            Thread.Sleep(1000);
            Monitoring.Received().HandleException(Arg.Any<string>());
        }

        [Then]
        public void EventuallyTheMessageEndsUpInErrorQueue()
        {
            _handler.WaitUntilCompletion(10.Seconds());
            _queue = new SqsQueueByName(string.Format("uk-int-{0}-1-customercommunication", _component), AWSClientFactory.CreateAmazonSQSClient(RegionEndpoint.EUWest1));
            _queue.Exists();

            var errorQueue = _queue.ErrorQueue;

            PatientlyAssert(() =>
            {
                errorQueue.Exists();
                var sqsMessageResponse =
                    errorQueue.Client.ReceiveMessage(new ReceiveMessageRequest
                    {
                        QueueUrl = errorQueue.Url,
                        MaxNumberOfMessages = 1,
                        WaitTimeSeconds = 1
                    });
               return sqsMessageResponse.Messages.Any();
            });
        }

        public void PatientlyAssert(Func<bool> expression)
        {
            bool result = false;
            bool timedOut;
            var started = DateTime.Now;
            do
            {
                try { result = expression.Invoke(); }
                catch { }
                timedOut = TimeSpan.FromSeconds(20) < DateTime.Now - started;
                Thread.Sleep(TimeSpan.FromSeconds(5));
                Console.WriteLine("{0} - Still Checking...", (DateTime.Now - started).TotalSeconds);
            } while (!result && !timedOut);

            Assert.True(result);
        }

        public override void PostAssertTeardown()
        {
            if(_queue!= null)
                 _queue.Delete();
        }
    }
}