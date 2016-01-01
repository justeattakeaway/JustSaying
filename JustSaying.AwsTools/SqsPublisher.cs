using Amazon.SQS;
using Amazon.SQS.Model;
using JustSaying.Messaging;
using JustSaying.Messaging.MessageSerialisation;
using Newtonsoft.Json;
using Message = JustSaying.Models.Message;

namespace JustSaying.AwsTools
{
    public class SqsPublisher : SqsQueueByName, IMessagePublisher
    {
        private readonly IAmazonSQS _client;
        private readonly IMessageSerialisationRegister _serialisationRegister;

        public SqsPublisher(string queueName, ISqsClient client, int retryCountBeforeSendingToErrorQueue, IMessageSerialisationRegister serialisationRegister)
            : base(queueName, client, retryCountBeforeSendingToErrorQueue)
        {
            _client = client;
            _serialisationRegister = serialisationRegister;
        }

        public void Publish(Message message)
        {
            _client.SendMessage(new SendMessageRequest
            {
                MessageBody = GetMessageInContext(message),
                QueueUrl = Url
            });
        }

        public string GetMessageInContext(Message message)
        {
            return _serialisationRegister.Serialise(message, serializeForSnsPublishing: false);
        }
    }
}