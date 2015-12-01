using Amazon;
using Amazon.SQS;

namespace JustSaying.AwsTools
{
    public interface ISqsClient : IAmazonSQS
    { 
        RegionEndpoint Region { get; }
    }
}