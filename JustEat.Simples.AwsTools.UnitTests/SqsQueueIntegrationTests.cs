using System.Configuration;
using System.Threading;
using Amazon;
using JustEat.Simples.NotificationStack.AwsTools;
using NUnit.Framework;

namespace AwsTools.UnitTests
{
    [TestFixture]
    public class SqsQueueIntegrationTests
    {
        [Test, Explicit]
        // Use this to manually test the creation of a queue.
        public void CreatingAQueue()
        {
            var q = new SqsQueueByName("testQ", AWSClientFactory.CreateAmazonSQSClient(RegionEndpoint.EUWest1));
            if (q.Exists())
            {
                q.Delete();
                Thread.Sleep(60000);
            }

            var x = q.Create(600);
            Thread.Sleep(10000);

            Assert.True(q.Exists());
        }
    }
}
