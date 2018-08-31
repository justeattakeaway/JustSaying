using Amazon;
using Amazon.Runtime;
using Amazon.Runtime.Internal;
using Amazon.SQS;

namespace JustSaying.Extensions
{
    internal sealed class CustomAmazonSQSClient : AmazonSQSClient
    {
        internal CustomAmazonSQSClient(AWSCredentials credentials, RegionEndpoint region)
            : base(credentials, region)
        {
        }

        protected override void CustomizeRuntimePipeline(RuntimePipeline pipeline)
        {
            this.ConfigureHttpHandler(pipeline);
            base.CustomizeRuntimePipeline(pipeline);
        }
    }
}
