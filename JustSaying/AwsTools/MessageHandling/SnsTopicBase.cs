using System;
using System.IO;
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
        private readonly IMessageSubjectProvider _messageSubjectProvider;
        private readonly SnsWriteConfiguration _snsWriteConfiguration;
        public Action<MessageResponse, Message> MessageResponseLogger { get; set; }
        public string Arn { get; protected set; }
        protected IAmazonSimpleNotificationService Client { get; set; }
        private readonly ILogger _eventLog;
        private readonly ILogger _log;

        protected SnsTopicBase(IMessageSerialisationRegister serialisationRegister, ILoggerFactory loggerFactory, IMessageSubjectProvider messageSubjectProvider)
        {
            _serialisationRegister = serialisationRegister;
            _messageSubjectProvider = messageSubjectProvider;
            _log = loggerFactory.CreateLogger("JustSaying");
            _eventLog = loggerFactory.CreateLogger("EventLog");
        }

        protected SnsTopicBase(IMessageSerialisationRegister serialisationRegister,
            ILoggerFactory loggerFactory, SnsWriteConfiguration snsWriteConfiguration,
            IMessageSubjectProvider messageSubjectProvider)
        {
            _serialisationRegister = serialisationRegister;
            _log = loggerFactory.CreateLogger("JustSaying");
            _eventLog = loggerFactory.CreateLogger("EventLog");
            _snsWriteConfiguration = snsWriteConfiguration;
            _messageSubjectProvider = messageSubjectProvider;
        }

        public abstract Task<bool> ExistsAsync();
        
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
            var messageType = _messageSubjectProvider.GetSubjectForType(message.GetType());

            var messageAttributeValues = message.MessageAttributes?.ToDictionary(
                source => source.Key,
                source =>
                {
                    if (source.Value == null)
                    {
                        return null;
                    }

                    var binaryValueStream = source.Value.BinaryValue != null
                        ? new MemoryStream(source.Value.BinaryValue.ToArray(), false)
                        : null;

                    return new MessageAttributeValue
                    {
                        StringValue = source.Value.StringValue,
                        BinaryValue = binaryValueStream,
                        DataType = source.Value.DataType
                    };
                });
            
            return new PublishRequest
            {
                TopicArn = Arn,
                Subject = messageType,
                Message = messageToSend,
                MessageAttributes = messageAttributeValues
            };
        }
    }
}
