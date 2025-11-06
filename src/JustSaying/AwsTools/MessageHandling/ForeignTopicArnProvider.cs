using System.Threading.Tasks;
using Amazon;

namespace JustSaying.AwsTools.MessageHandling
{
    internal class ForeignTopicArnProvider : ITopicArnProvider
    {

        private readonly string _arn;

        public ForeignTopicArnProvider(RegionEndpoint regionEndpoint, string accountId, string topicName)
        {
            _arn = $"arn:aws:sns:{regionEndpoint.SystemName}:{accountId}:{topicName}";
        }

        public Task<bool> ArnExistsAsync()
        {
            // Assume foreign topics exist, we actually find out when we attempt to subscribe
            return Task.FromResult(true);
        }

        public Task<string> GetArnAsync()
        {
            return Task.FromResult(_arn);
        }
    }
}
