using Amazon;
using Amazon.Runtime;
using Amazon.Runtime.Internal;
using Amazon.SimpleNotificationService;

namespace JustSaying.Extensions
{
    internal sealed class CustomAmazonSimpleNotificationServiceClient : AmazonSimpleNotificationServiceClient
    {
        internal CustomAmazonSimpleNotificationServiceClient(AWSCredentials credentials, RegionEndpoint region)
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
