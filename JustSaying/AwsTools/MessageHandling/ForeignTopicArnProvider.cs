using System.Threading.Tasks;
using Amazon;

namespace JustSaying.AwsTools.MessageHandling
{
    public class ForeignTopicArnProvider : ITopicArnProvider
    {

        private readonly string _arn;

        public ForeignTopicArnProvider(RegionEndpoint regionEndpoint, string accountId, string topicName)
        {
            _arn = $"arn:aws:sns:{regionEndpoint.SystemName}:{accountId}:{topicName}";
        }

        // Assume foreign topics exist, we actually find out when we attempt to subscribe
        public bool ArnExists() => true;

        public Task<string> GetArnAsync() => Task.FromResult(_arn);
    }
}
