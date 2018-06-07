using System;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Amazon.SimpleNotificationService;
using Amazon.SimpleNotificationService.Model;
using JustSaying.AwsTools.QueueCreation;
using JustSaying.Messaging;
using JustSaying.Messaging.MessageSerialisation;
using Microsoft.Extensions.Logging;
using Message = JustSaying.Models.Message;

namespace JustSaying.AwsTools.MessageHandling
{
    public abstract class SnsTopicBase : IMessagePublisher
    {
        private readonly IMessageSerialisationRegister _serialisationRegister; // ToDo: Grrr...why is this here even. GET OUT!
        private readonly SnsWriteConfiguration _snsWriteConfiguration;
        public Action<MessageResponse, Message> MessageResponseLogger { get; set; }
        public string Arn { get; protected set; }
        protected IAmazonSimpleNotificationService Client { get; set; }
        private readonly ILogger _eventLog;
        private readonly ILogger _log;

        protected SnsTopicBase(IMessageSerialisationRegister serialisationRegister, ILoggerFactory loggerFactory)
        {
            _serialisationRegister = serialisationRegister;
            _log = loggerFactory.CreateLogger("JustSaying");
            _eventLog = loggerFactory.CreateLogger("EventLog");
        }

        protected SnsTopicBase(IMessageSerialisationRegister serialisationRegister,
            ILoggerFactory loggerFactory, SnsWriteConfiguration snsWriteConfiguration)
        {
            _serialisationRegister = serialisationRegister;
            _log = loggerFactory.CreateLogger("JustSaying");
            _eventLog = loggerFactory.CreateLogger("EventLog");
            _snsWriteConfiguration = snsWriteConfiguration;
        }

        public abstract Task<bool> ExistsAsync();

        public Task<bool> SubscribeAsync(SqsQueueBase queue) => SubscribeAsync(queue, CancellationToken.None);

        public async Task<bool> SubscribeAsync(SqsQueueBase queue, CancellationToken cancellationToken)
        {
            var subscriptionResponse = await Client.SubscribeAsync(Arn, "sqs", queue.Arn, cancellationToken).ConfigureAwait(false);

            if (!string.IsNullOrEmpty(subscriptionResponse?.SubscriptionArn))
            {
                return true;
            }

            _log.LogInformation($"Failed to subscribe Queue to Topic: {queue.Arn}, Topic: {Arn}");
            return false;
        }

#if AWS_SDK_HAS_SYNC
        public void Publish(Message message)
        {
            var request = BuildPublishRequest(message);

            try
            {
                var response = Client.Publish(request);
                _eventLog.LogInformation($"Published message: '{request.Subject}' with content {request.Message}");

                MessageResponseLogger?.Invoke(new MessageResponse
                {
                    HttpStatusCode = response?.HttpStatusCode,
                    MessageId = response?.MessageId
                }, message);
            }
            catch (Exception ex)
            {
                if (!ClientExceptionHandler(ex, message))
                    throw new PublishException(
                        $"Failed to publish message to SNS. TopicArn: {request.TopicArn} Subject: {request.Subject} Message: {request.Message}",
                        ex);
            }
        }
#endif

        public Task PublishAsync(Message message) => PublishAsync(message, CancellationToken.None);

        public async Task PublishAsync(Message message, CancellationToken cancellationToken)
        {
            var request = BuildPublishRequest(message);

            try
            {
                var response = await Client.PublishAsync(request, cancellationToken).ConfigureAwait(false);
                _eventLog.LogInformation($"Published message: '{request.Subject}' with content {request.Message}");

                MessageResponseLogger?.Invoke(new MessageResponse
                {
                    HttpStatusCode = response?.HttpStatusCode,
                    MessageId = response?.MessageId
                }, message);
            }
            catch (Exception ex)
            {
                if (!ClientExceptionHandler(ex, message))
                    throw new PublishException(
                        $"Failed to publish message to SNS. TopicArn: {request.TopicArn} Subject: {request.Subject} Message: {request.Message}",
                        ex);
            }
        }

        private bool ClientExceptionHandler(Exception ex, Message message) => _snsWriteConfiguration?.HandleException?.Invoke(ex, message) ?? false;

        private PublishRequest BuildPublishRequest(Message message)
        {
            var messageToSend = _serialisationRegister.Serialise(message, serializeForSnsPublishing: true);
            var messageType = message.GetType().Name;

            return new PublishRequest
            {
                TopicArn = Arn,
                Subject = messageType,
                Message = messageToSend,
                MessageAttributes = message.MessageAttributes
            };
        }
    }
}
