using Amazon;

namespace JustSaying.AwsTools.MessageHandling;

internal class ForeignTopicArnProvider(RegionEndpoint regionEndpoint, string accountId, string topicName) : ITopicArnProvider
{

    private readonly string _arn = $"arn:aws:sns:{regionEndpoint.SystemName}:{accountId}:{topicName}";

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
