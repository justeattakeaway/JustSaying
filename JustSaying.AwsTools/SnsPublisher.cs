using Amazon.SimpleNotificationService;
using Amazon.SimpleNotificationService.Model;
using Amazon.SQS;
using NLog;

namespace JustSaying.AwsTools
{
    public class SnsPublisher : IPublisher
    {
        public string Arn { get; private set; }
        public IAmazonSimpleNotificationService Client { get; private set; }
        public string TopicName { get; private set; }

        private static readonly Logger Log = LogManager.GetLogger("JustSaying");
        private static readonly Logger EventLog = LogManager.GetLogger("EventLog");

        public SnsPublisher(string topicName, IAmazonSimpleNotificationService client)
        {
            TopicName = topicName;
            Client = client;
        }

        public void Subscribe(IAmazonSQS amazonSQSClient, SqsQueueBase queue)
        {
            var subscriptionArn = Client.SubscribeQueue(Arn, amazonSQSClient, queue.Url);
            if (!string.IsNullOrEmpty(subscriptionArn))
            {
                return;
            }

            Log.Info(string.Format("Failed to subscribe Queue to Topic: {0}, Topic: {1}", queue.Arn, Arn));
        }

        public void Publish(string subject, string message)
        {
            Client.Publish(new PublishRequest
            {
                Subject = subject,
                Message = message,
                TopicArn = Arn
            });

            EventLog.Info("Published message: '{0}' with content {1}", subject, message);
        }

        public void Configure()
        {
            if (!Exists())
            {
                Create();
            }
        }

        private bool Exists()
        {
            var topic = Client.FindTopic(TopicName);

            if (topic != null)
            {
                Arn = topic.TopicArn;
                return true;
            }
            
            return false;
        }

        private void Create()
        {
            var response = Client.CreateTopic(new CreateTopicRequest(TopicName));
            if (!string.IsNullOrEmpty(response.TopicArn))
            {    
                Arn = response.TopicArn;
                Log.Info(string.Format("Created Topic: {0} on Arn: {1}", TopicName, Arn));
                return;
            }
            Log.Info(string.Format("Failed to create Topic: {0}", TopicName));
        }
    }
}