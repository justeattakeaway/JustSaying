using System;
using System.Threading.Tasks;
using Amazon;
using Amazon.SQS;
using Amazon.SQS.Model;
using JustSaying.Messaging;
using JustSaying.Messaging.MessageSerialisation;
using Message = JustSaying.Models.Message;

namespace JustSaying.AwsTools.MessageHandling
{
    public class SqsPublisher : SqsQueueByName, IMessagePublisher
    {
        private readonly IAmazonSQS _client;
        private readonly IMessageSerialisationRegister _serialisationRegister;

        public SqsPublisher(RegionEndpoint region, string queueName, IAmazonSQS client, int retryCountBeforeSendingToErrorQueue, IMessageSerialisationRegister serialisationRegister)
            : base(region, queueName, client, retryCountBeforeSendingToErrorQueue)
        {
            _client = client;
            _serialisationRegister = serialisationRegister;
        }

        public async Task Publish(Message message)
        {
            var request = new SendMessageRequest
                {
                    MessageBody = GetMessageInContext(message),
                    QueueUrl = Url
                };

            try
            {
                await _client.SendMessageAsync(request)
                    .ConfigureAwait(false);
            }
            catch (Exception ex)
            {
                throw new Exception($"Failed to publish message to SQS. QueueUrl: {request.QueueUrl} MessageBody: {request.MessageBody}", ex);
            }
        }

        public string GetMessageInContext(Message message) => _serialisationRegister.Serialise(message, serializeForSnsPublishing: false);
    }
}