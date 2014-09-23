namespace JustSaying.Lookups
{
    public class SqsPublishEndpointProvider : IPublishEndpointProvider
    {
        private readonly string _queueName;

        public SqsPublishEndpointProvider(string queueName)
        {
            _queueName = queueName;
        }

        public string GetLocationName()
        {
            return _queueName.ToLower();
        }
    }
}