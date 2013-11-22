using System;
using System.Threading;
using Amazon;
using JustEat.Simples.NotificationStack.AwsTools;
using NUnit.Framework;

namespace AwsTools.IntegrationTests
{
    [TestFixture]
    public class SqsQueueIntegrationTests
    {
        private SqsQueueByName _queue;
        private string _queueUniqueKey;

        [SetUp]
        public void DeleteAnyExistingQueue()
        {
            _queueUniqueKey = "test" + DateTime.Now.Ticks.ToString();
            _queue = new SqsQueueByName(_queueUniqueKey, AWSClientFactory.CreateAmazonSQSClient(RegionEndpoint.EUWest1));
        }

        [Test]
        public void CreatingAQueue()
        {
            Assert.True(_queue.Create(600));
            AssertCreated();
        }

        [TestFixtureTearDown]
        public void Teardown()
        {
            if (_queue.Exists())
                _queue.Delete();
        }

        private void AssertCreated()
        {
            bool created;
            bool timedOut;
            var started = DateTime.Now;
            do
            {
                created = _queue.Exists();
                timedOut = TimeSpan.FromSeconds(90) < DateTime.Now - started;
                Thread.Sleep(TimeSpan.FromSeconds(5));
                Console.WriteLine("{0} - Still Checking...", (DateTime.Now - started).TotalSeconds);
            } while (!created && !timedOut);

            Assert.True(created);
        }
    }
}
