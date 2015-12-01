using Amazon;
using Amazon.SQS;

namespace JustSaying.AwsTools
{
    public class SqsClient : AmazonSQSClient, ISqsClient
    {
        public SqsClient(RegionEndpoint regionEndpoint)
            : base(regionEndpoint)
        {
            Region = regionEndpoint;
        }

        public RegionEndpoint Region { get; private set; }
    }
}