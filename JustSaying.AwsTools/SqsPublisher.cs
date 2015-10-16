using Amazon.SQS;
using Amazon.SQS.Model;
using JustSaying.Messaging;
using JustSaying.Messaging.Extensions;
using JustSaying.Messaging.MessageSerialisation;
using Newtonsoft.Json;
using Message = JustSaying.Models.Message;

namespace JustSaying.AwsTools
{
    public class SqsPublisher : SqsQueueByName, IMessagePublisher
    {
        private readonly IAmazonSQS _client;
        private readonly IMessageSerialisationRegister _serialisationRegister;

        public SqsPublisher(string queueName, IAmazonSQS client, int retryCountBeforeSendingToErrorQueue, IMessageSerialisationRegister serialisationRegister)
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

        private string GetMessageInContext(Message message)
        {
            var serializedMessage = _serialisationRegister.GeTypeSerialiser(message.GetType()).Serialiser.Serialise(message);
            var context = new { Subject = message.GetType().ToKey(), Message = serializedMessage };
            return JsonConvert.SerializeObject(context);
        }
    }
}