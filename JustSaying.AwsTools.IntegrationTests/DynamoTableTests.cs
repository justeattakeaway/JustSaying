using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Amazon;
using Amazon.DynamoDBv2;
using Amazon.DynamoDBv2.Model;
using JustSaying.AwsTools;
using NUnit.Framework;

namespace JustSaying.AwsTools.IntegrationTests
{
    [TestFixture]
    public class DynamoTableTests
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