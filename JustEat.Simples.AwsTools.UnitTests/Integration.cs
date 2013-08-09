using System;
using System.Configuration;
using System.Threading;
using Amazon;
using Amazon.DynamoDBv2;
using Amazon.DynamoDBv2.DataModel;
using JustEat.Simples.NotificationStack.AwsTools;
using NUnit.Framework;

namespace AwsTools.UnitTests
{
    [TestFixture]
    public class Integration
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

        [Test, Explicit]
        public void SavingAndRetrievingAnItemInDynamoDb()
        {
            var client = new AmazonDynamoDBClient(RegionEndpoint.EUWest1);
            var provider = new DynamoStore(new DynamoDBContext(client));
            var defaultDynamoDbOperationConfig = new DynamoDBOperationConfig();
            var key = Guid.NewGuid().ToString();
            var item = new MessageFootprint() { MessageId = key };

            provider.Save(item, defaultDynamoDbOperationConfig);

            var footprint = provider.Read<MessageFootprint>(key, defaultDynamoDbOperationConfig);
            Assert.IsNotNull(footprint);
            Console.WriteLine(footprint.MessageId);
        }
        
    }

}
