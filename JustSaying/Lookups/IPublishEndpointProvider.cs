using JustSaying.AwsTools.QueueCreation;

namespace JustSaying.Lookups
{
    public interface IPublishEndpointProvider
    {
        string GetLocationName();
    }

    public class SnsPublishEndpointProvider : IPublishEndpointProvider
    {
        private readonly string _topic;

        public SnsPublishEndpointProvider(string topic)
        {
            _topic = topic;
        }

        public string GetLocationName()
        {
            return _topic.ToLower();
        }
    }
}