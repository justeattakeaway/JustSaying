using System;
using Amazon;
using Amazon.DynamoDBv2;
using Amazon.DynamoDBv2.DataModel;
using JustEat.Simples.NotificationStack.AwsTools;
using NUnit.Framework;

namespace AwsTools.IntegrationTests
{
    [TestFixture]
    public class DynamoDbStoreIntregrationTests
    {
        [Test]
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