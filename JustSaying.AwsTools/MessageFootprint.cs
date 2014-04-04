using System;
using Amazon.DynamoDBv2.DataModel;

namespace JustSaying.AwsTools
{
    [DynamoDBTable("MessageFootprint")]
    public class MessageFootprint
    {
        [DynamoDBHashKey("MessageId")]
        public string MessageId { get; set; }
    }
    
}
