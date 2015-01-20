using Amazon.SQS;
using Amazon.SQS.Model;
using Newtonsoft.Json;

namespace JustSaying.AwsTools
{
    public class SqsPublisher : SqsQueueByName, IPublisher
    {
        private readonly IAmazonSQS _client;

        public SqsPublisher(string queueName, IAmazonSQS client, int retryCountBeforeSendingToErrorQueue)
            : base(queueName, client, retryCountBeforeSendingToErrorQueue)
        {
            _client = client;
        }

        private static string GetMessageInContext(string subject, string message)
        {
            // ToDo: No no mr JsonConvert.
            var context = new { Subject = subject, Message = message };
            return JsonConvert.SerializeObject(context);
        }

        public void Publish(string subject, string message)
        {
            _client.SendMessage(new SendMessageRequest
            {
                MessageBody = GetMessageInContext(subject, message),
                QueueUrl = Url
            });
        }

        public void Configure()
        {
            throw new System.NotImplementedException();
        }
    }
}