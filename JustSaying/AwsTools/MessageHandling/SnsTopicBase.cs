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
using JustSaying.Models;
using Microsoft.Extensions.Logging;
using Message = JustSaying.Models.Message;
using MessageAttributeValue = Amazon.SimpleNotificationService.Model.MessageAttributeValue;

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

        protected SnsTopicBase(IMessageSerialisationRegister serialisationRegister, ILoggerFactory loggerFactory, IMessageSubjectProvider messageSubjectProvider)
        {
            _serialisationRegister = serialisationRegister;
            _messageSubjectProvider = messageSubjectProvider;
            _eventLog = loggerFactory.CreateLogger("EventLog");
        }

        protected SnsTopicBase(IMessageSerialisationRegister serialisationRegister,
            ILoggerFactory loggerFactory, SnsWriteConfiguration snsWriteConfiguration,
            IMessageSubjectProvider messageSubjectProvider)
        {
            _serialisationRegister = serialisationRegister;
            _eventLog = loggerFactory.CreateLogger("EventLog");
            _snsWriteConfiguration = snsWriteConfiguration;
            _messageSubjectProvider = messageSubjectProvider;
        }

        public abstract Task<bool> ExistsAsync();
        
        public async Task PublishAsync(PublishEnvelope env, CancellationToken cancellationToken)
        {
            var request = BuildPublishRequest(env);

            try
            {
                var response = await Client.PublishAsync(request, cancellationToken).ConfigureAwait(false);
                _eventLog.LogInformation($"Published message: '{request.Subject}' with content {request.Message}");

                MessageResponseLogger?.Invoke(new MessageResponse
                {
                    HttpStatusCode = response?.HttpStatusCode,
                    MessageId = response?.MessageId
                }, env.Message);
            }
            catch (Exception ex)
            {
                if (!ClientExceptionHandler(ex, env.Message))
                    throw new PublishException(
                        $"Failed to publish message to SNS. TopicArn: {request.TopicArn} Subject: {request.Subject} Message: {request.Message}",
                        ex);
            }
        }

        private bool ClientExceptionHandler(Exception ex, Message message) => _snsWriteConfiguration?.HandleException?.Invoke(ex, message) ?? false;

        private PublishRequest BuildPublishRequest(PublishEnvelope env)
        {
            var messageToSend = _serialisationRegister.Serialise(env.Message, serializeForSnsPublishing: true);
            var messageType = _messageSubjectProvider.GetSubjectForType(env.Message.GetType());

            var messageAttributeValues = env.MessageAttributes?.ToDictionary(
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
