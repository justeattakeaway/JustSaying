using Amazon;

namespace JustSaying.AwsTools.MessageHandling;

internal class ForeignTopicArnProvider(RegionEndpoint regionEndpoint, string accountId, string topicName)
{
    private readonly string _arn = $"arn:aws:sns:{regionEndpoint.SystemName}:{accountId}:{topicName}";

    public Task<string> GetArnAsync()
    {
        return Task.FromResult(_arn);
    }
}
