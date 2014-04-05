using System.Collections.Generic;
using Amazon.DynamoDBv2.Model;

namespace JustSaying.AwsTools
{
    public class DynamoDbConfig
    {
        public string TableName { get; set; }
        public List<AttributeDefinition> AttributeDefinitions { get; set; }
        public List<KeySchemaElement> KeySchema { get; set; }
        public ProvisionedThroughput ProvisionedThroughput { get; set; }
    }
}