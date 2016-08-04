using Amazon.SimpleNotificationService;

namespace JustSaying.AwsTools.MessageHandling
{
    public class ForeignTopicArnProvider : ITopicArnProvider
    {
        private readonly string _arn;

        public ForeignTopicArnProvider(string topicName, string region, string accountId)
        {
            _arn = $"arn:aws:sns:{region}:{accountId}:{topicName}";
        }

        public bool ArnExists()
        {
            // Assume foreign topics exist, we actually find out when we attempt to subscribe
            return true;
        }

        public string GetArn()
        {
            return _arn;
        }
    }
}
