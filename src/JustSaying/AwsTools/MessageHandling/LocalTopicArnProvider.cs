using Amazon.SimpleNotificationService;

namespace JustSaying.AwsTools.MessageHandling;

internal class LocalTopicArnProvider : ITopicArnProvider
{
    private readonly IAmazonSimpleNotificationService _client;
    private readonly Lazy<Task<string>> _lazyGetArnAsync;
    private bool _exists;

    public LocalTopicArnProvider(IAmazonSimpleNotificationService client, string topicName)
    {
        _client = client;

        _lazyGetArnAsync = new Lazy<Task<string>>(() => GetArnInternalAsync(topicName));
    }

    private async Task<string> GetArnInternalAsync(string topicName)
    {
        try
        {
            var topic = await _client.FindTopicAsync(topicName).ConfigureAwait(false);

            _exists = true;
            return topic.TopicArn;
        }
        catch (Exception)
        {
            // ignored
        }
        return null;
    }

    public Task<string> GetArnAsync()
    {
        return _lazyGetArnAsync.Value;
    }

    public async Task<bool> ArnExistsAsync()
    {
        _ = await _lazyGetArnAsync.Value.ConfigureAwait(false);
        return _exists;
    }
}
