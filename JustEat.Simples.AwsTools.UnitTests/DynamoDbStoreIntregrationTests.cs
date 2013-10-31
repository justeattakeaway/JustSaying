using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Amazon;
using Amazon.DynamoDBv2;
using Amazon.DynamoDBv2.DataModel;
using Amazon.DynamoDBv2.Model;
using JustEat.Simples.NotificationStack.AwsTools;
using NUnit.Framework;

namespace AwsTools.UnitTests
{
    [TestFixture]
    public class DynamoDbStoreIntregrationTests
    {
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

    [TestFixture]
    public class DynamoDbTests
    {
        private string _tableName;
        private AmazonDynamoDBClient _client;

        [SetUp]
        public void SetUp()
        {
            _client = new AmazonDynamoDBClient(RegionEndpoint.EUWest1);
            _tableName = "Test-" + Guid.NewGuid();
        }

        [Test]
        public void CreateDynamoDbIfDoesNotExist()
        {
            DynamoDbConfig config = GetDynamoDbConfig();
            DynamoTable table = new DynamoTable(config, _client);

            table.CreateIfNotExist();

            AssertDynamoDbExists(_tableName);
        }

        [Test]
        public void CanHandleConcurrentCalls()
        {
            DynamoDbConfig config = GetDynamoDbConfig();

            var table = new DynamoTable(config, _client);
            var tasks = Enumerable.Repeat(Task.Factory.StartNew(table.CreateIfNotExist), 10);
            Task.WaitAll(tasks.ToArray());

            AssertDynamoDbExists(_tableName);
        }

        private DynamoDbConfig GetDynamoDbConfig()
        {
            return new DynamoDbConfig
                {
                    TableName = _tableName,
                    AttributeDefinitions = new List<AttributeDefinition>()
                        {
                            new AttributeDefinition
                                {
                                    AttributeName = "Id",
                                    AttributeType = "N"
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

        private void AssertDynamoDbExists(string tableName)
        {
            var request = new DescribeTableRequest { TableName = tableName };
            try
            {
                var response = _client.DescribeTable(request);
            }
            catch (ResourceNotFoundException)
            {

                Assert.Fail("Table {0} doesn't exist!", _tableName);
            }
        }

        [TearDown]
        public void Cleanup()
        {
            try
            {
                var request = new DeleteTableRequest { TableName = _tableName };
                var response = _client.DeleteTable(request);
            }
            catch { }
        }
    }
}