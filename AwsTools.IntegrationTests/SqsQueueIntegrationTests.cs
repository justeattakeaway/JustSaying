using System;
using System.Threading;
using Amazon;
using JustEat.Simples.NotificationStack.AwsTools;
using JustEat.Testing;
using NUnit.Framework;

namespace AwsTools.IntegrationTests
{
    public abstract class WhenCreatingQueuesByName : BehaviourTest<SqsQueueByName>
    {
        protected string _queueUniqueKey;

        protected override void Given()
        { }

        public void PatientlyAssert(Func<bool> expression)
        {
            bool result;
            bool timedOut;
            var started = DateTime.Now;
            do
            {
                result = expression.Invoke();
                timedOut = TimeSpan.FromSeconds(90) < DateTime.Now - started;
                Thread.Sleep(TimeSpan.FromSeconds(5));
                Console.WriteLine("{0} - Still Checking...", (DateTime.Now - started).TotalSeconds);
            } while (!result && !timedOut);

            Assert.True(result);
        }

        protected override SqsQueueByName CreateSystemUnderTest()
        {
            _queueUniqueKey = "test" + DateTime.Now.Ticks;
            return new SqsQueueByName(_queueUniqueKey, AWSClientFactory.CreateAmazonSQSClient(RegionEndpoint.EUWest1));
        }
        public override void PostAssertTeardown()
        {
            SystemUnderTest.Delete();
            base.PostAssertTeardown();
        }
    }

    public class WhenICreateAQueueByName : WhenCreatingQueuesByName
    {
        private bool _isQueueCreated;

        protected override void When()
        {
            _isQueueCreated = SystemUnderTest.Create(60, 0, 30);
        }

        [Then]
        public void TheQueueISCreated()
        {
            Assert.IsTrue(_isQueueCreated);
        }

        [Then]
        public void DeadLetterQueueIsCreated()
        {
            PatientlyAssert(() => SystemUnderTest.ErrorQueue.Exists());
        }
    }

    public class WhenIAccessAnExistingQueueWithoutAnErrorQueue : WhenCreatingQueuesByName
    {
        protected override void When()
        {
            SystemUnderTest.Create(600, 0, 30, createErrorQueue: false);
        }

        [Then]
        public void ThereIsNoErrorQueue()
        {
            PatientlyAssert(() => !SystemUnderTest.ErrorQueue.Exists());
        }
    }

    public class WhenQueueIsDeleted : WhenCreatingQueuesByName
    {
        protected override void When()
        {
            SystemUnderTest.Create(600);
            SystemUnderTest.Delete();
        }

        [Test]
        public void TheErrorQueueIsDeleted()
        {
            PatientlyAssert(() => !SystemUnderTest.ErrorQueue.Exists());
        }
    }
}
