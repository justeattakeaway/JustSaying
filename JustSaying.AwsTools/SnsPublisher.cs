using Amazon.SimpleNotificationService;
using Amazon.SimpleNotificationService.Model;
using Amazon.SQS;
using NLog;

namespace JustSaying.AwsTools
{
    public class SnsPublisher : IPublisher
    {
        public string Arn { get; protected set; }
        public IAmazonSimpleNotificationService Client { get; protected set; }
        private static readonly Logger EventLog = LogManager.GetLogger("EventLog");
        public string TopicName { get; private set; }
        private static readonly Logger Log = LogManager.GetLogger("JustSaying");

        public SnsPublisher(string topicName, IAmazonSimpleNotificationService client)
        {
            TopicName = topicName;
            Client = client;
            Exists();
        }

        public bool Subscribe(IAmazonSQS amazonSQSClient, SqsQueueBase queue)
        {
            var subscriptionArn = Client.SubscribeQueue(Arn, amazonSQSClient, queue.Url);
            if (!string.IsNullOrEmpty(subscriptionArn))
            {
                return true;
            }

            Log.Info(string.Format("Failed to subscribe Queue to Topic: {0}, Topic: {1}", queue.Arn, Arn));
            return false;
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

        public bool Exists()
        {
            var topic = Client.FindTopic(TopicName);

            if (topic != null)
            {
                Arn = topic.TopicArn;
                return true;
            }
            
            return false;
        }

        public bool Create()
        {
            var response = Client.CreateTopic(new CreateTopicRequest(TopicName));
            if (!string.IsNullOrEmpty(response.TopicArn))
            {    
                Arn = response.TopicArn;
                Log.Info(string.Format("Created Topic: {0} on Arn: {1}", TopicName, Arn));
                return true;
            }
            Log.Info(string.Format("Failed to create Topic: {0}", TopicName));
            return false;
        }
    }
}