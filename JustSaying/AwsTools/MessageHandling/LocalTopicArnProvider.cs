using System;
using System.Runtime.CompilerServices;
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

            _lazyGetArn = new AsyncLazy<string>(() => GetArnInternal(topicName));
        }

        private async Task<string> GetArnInternal(string topicName)
        {
            try
            {
                var topic = await _client.FindTopicAsync(topicName);

                _exists = true;
                return topic.TopicArn;
            }
            catch
            {
                // ignored
            }
            return null;
        }

        public async Task<string> GetArnAsync() => await _lazyGetArn;

        public bool ArnExists()
        {
            // ReSharper disable once UnusedVariable
            var ignored = _lazyGetArn.Value;
            return _exists;
        }

        private class AsyncLazy<T> : Lazy<Task<T>>
        {
            public AsyncLazy(Func<Task<T>> taskFactory) :
                base(() => Task.Factory.StartNew(taskFactory).Unwrap())
            { }

            public TaskAwaiter<T> GetAwaiter() => Value.GetAwaiter();
        }
    }
}
