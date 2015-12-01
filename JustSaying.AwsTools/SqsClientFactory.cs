using Amazon;

namespace JustSaying.AwsTools
{
    public static class SqsClientFactory
    {
        public static ISqsClient Create(RegionEndpoint regionEndpoint)
        {
            return new SqsClient(regionEndpoint);
        }
    }
}
