using System;
using System.Threading.Tasks;
using Amazon.SimpleNotificationService;

namespace JustSaying.AwsTools.MessageHandling
{
    public class LocalTopicArnProvider : ITopicArnProvider
    {
        private readonly IAmazonSimpleNotificationService _client;
        private readonly AsyncLazy<string> _lazyGetArn;
        private bool _exists;

        public LocalTopicArnProvider(IAmazonSimpleNotificationService client, string topicName)
        {
            _client = client;

            _lazyGetArn = new AsyncLazy<string>(() => GetArnInternalAsync(topicName));
        }

        private async Task<string> GetArnInternalAsync(string topicName)
        {
            try
            {
                var topic = await _client.FindTopicAsync(topicName).ConfigureAwait(false);

                _exists = true;
                return topic.TopicArn;
            }
            catch
            {
                // ignored
            }
            return null;
        }

        public Task<string> GetArnAsync()
        {
            return _lazyGetArn.Value;
        }

        public async Task<bool> ArnExistsAsync()
        {
            // ReSharper disable once UnusedVariable
            var ignored = await _lazyGetArn.Value.ConfigureAwait(false);
            return _exists;
        }
    }
}
