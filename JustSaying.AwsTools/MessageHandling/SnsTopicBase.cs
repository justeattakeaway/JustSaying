using System;
using System.Linq;
using System.Threading.Tasks;
using Amazon.SimpleNotificationService;
using Amazon.SimpleNotificationService.Model;
using Amazon.SQS;
using JustSaying.Messaging;
using JustSaying.Messaging.MessageSerialisation;
using NLog;
using Message = JustSaying.Models.Message;

namespace JustSaying.AwsTools.MessageHandling
{
    public abstract class SnsTopicBase : IMessagePublisher
    {
        private readonly IMessageSerialisationRegister _serialisationRegister; // ToDo: Grrr...why is this here even. GET OUT!
        public string Arn { get; protected set; }
        public IAmazonSimpleNotificationService Client { get; protected set; }
        private static readonly Logger EventLog = LogManager.GetLogger("EventLog");
        private static readonly Logger Log = LogManager.GetLogger("JustSaying");

        public SnsTopicBase(IMessageSerialisationRegister serialisationRegister)
        {
            _serialisationRegister = serialisationRegister;
        }

        public abstract Task<bool> ExistsAsync();

        public bool Exists()
        {
            return ExistsAsync()
                .GetAwaiter().GetResult();
        }

        public bool IsSubscribed(SqsQueueBase queue)
        {
            // todo make async
            var result = Client.ListSubscriptionsByTopicAsync(new ListSubscriptionsByTopicRequest(Arn))
                .GetAwaiter().GetResult();

            return result.Subscriptions.Any(x => !string.IsNullOrEmpty(x.SubscriptionArn) && x.Endpoint == queue.Arn);
        }

        public bool Subscribe(IAmazonSQS amazonSqsClient, SqsQueueBase queue)
        {
            // todo make async
            var subscriptionResponse = Client.SubscribeAsync(Arn, "sqs", queue.Arn)
                .GetAwaiter().GetResult();

            if (!string.IsNullOrEmpty(subscriptionResponse?.SubscriptionArn))
            {
                return true;
            }

            Log.Info($"Failed to subscribe Queue to Topic: {queue.Arn}, Topic: {Arn}");
            return false;
        }

        public void Publish(Message message)
        {
            PublishAsync(message)
                .GetAwaiter().GetResult();
        }

        private async Task PublishAsync(Message message)
        {
            var messageToSend = _serialisationRegister.Serialise(message, serializeForSnsPublishing: true);
            var messageType = message.GetType().Name;
            var request = new PublishRequest
                {
                    TopicArn = Arn,
                    Subject = messageType,
                    Message = messageToSend
                };

            try
            {
                await Client.PublishAsync(request);
                EventLog.Info($"Published message: '{messageType}' with content {messageToSend}");
            }
            catch (Exception ex)
            {
                throw new PublishException(
                    $"Failed to publish message to SNS. TopicArn: {request.TopicArn} Subject: {request.Subject} Message: {request.Message}",
                    ex);
            }

        }
    }
}
