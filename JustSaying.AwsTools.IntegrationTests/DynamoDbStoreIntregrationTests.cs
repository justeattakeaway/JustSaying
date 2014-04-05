using System;
using System.Collections.Generic;
using Amazon;
using Amazon.DynamoDBv2;
using Amazon.DynamoDBv2.DataModel;
using Amazon.DynamoDBv2.Model;
using JustSaying.AwsTools;
using NUnit.Framework;

namespace JustSaying.AwsTools.IntegrationTests
{
    [TestFixture]
    public class DynamoDbStoreIntregrationTests
    {

        [Test]
        public void CanReadAndWriteData()
        {
            var client = new AmazonDynamoDBClient(RegionEndpoint.EUWest1);
            const string itemId = "1";
            var provider = new DynamoStore(new DynamoDBContext(client));
            var config = GetDynamoDbConfig();
            var table = new DynamoTable(config, client);

            table.CreateIfNotExist();

            provider.Save(new FakeDynamoItem { Id = itemId }, new DynamoDBOperationConfig());
            var result = provider.Read<FakeDynamoItem>(itemId, new DynamoDBOperationConfig());

            Assert.IsNotNull(result);
            Console.WriteLine(result.Id);
            Assert.AreEqual(result.Id, itemId);
        }

        private DynamoDbConfig GetDynamoDbConfig()
        {
            return new DynamoDbConfig
            {
                TableName = "FakeDynamoItem",
                AttributeDefinitions = new List<AttributeDefinition>()
                        {
                            new AttributeDefinition
                                {
                                    AttributeName = "Id",
                                    AttributeType = "S"
                                }
                        },
                KeySchema = new List<KeySchemaElement>
                        {
                            new KeySchemaElement
                                {
                                    AttributeName = "Id",
                                    KeyType = "HASH"
                                }
                        },
                ProvisionedThroughput = new ProvisionedThroughput
                {
                    ReadCapacityUnits = 1,
                    WriteCapacityUnits = 1
                }
            };
        }

        [DynamoDBTable("FakeDynamoItem")]
        private class FakeDynamoItem
        {
            [DynamoDBHashKey("Id")]
            public string Id { get; set; }
        }
    }
}