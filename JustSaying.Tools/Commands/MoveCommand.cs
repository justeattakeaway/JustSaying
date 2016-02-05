using System;
using System.Collections.Generic;
using System.Linq;
using Amazon;
using Amazon.EC2;
using Amazon.Runtime;
using Amazon.SQS;
using Amazon.SQS.Model;
using JustSaying.AwsTools;
using JustSaying.AwsTools.MessageHandling;

namespace JustSaying.Tools.Commands
{
    public class MoveCommand : ICommand
    {
        public MoveCommand(string sourceQueueName, string destinationQueueName, int count)
        {
            SourceQueueName = sourceQueueName;
            DestinationQueueName = destinationQueueName;
            Count = count;
        }

        public string SourceQueueName { get; set; }
        public string DestinationQueueName { get; set; }
        public int Count { get; set; }

        public bool Execute()
        {
            Console.WriteLine("Moving {0} messages from {1} to {2}", Count, SourceQueueName, DestinationQueueName);

            var config = new AmazonSQSConfig();
            var client = new DefaultAwsClientFactory().GetSqsClient(config.RegionEndpoint);
            var sourceQueue = new SqsQueueByName(config.RegionEndpoint, SourceQueueName, client, JustSayingConstants.DEFAULT_HANDLER_RETRY_COUNT);
            var destinationQueue = new SqsQueueByName(config.RegionEndpoint, DestinationQueueName, client, JustSayingConstants.DEFAULT_HANDLER_RETRY_COUNT);

            var messages = PopMessagesFromSourceQueue(sourceQueue);
            var receiptHandles = messages.ToDictionary(m => m.MessageId, m => m.ReceiptHandle);
            
            var sendResponse = destinationQueue.Client.SendMessageBatch(new SendMessageBatchRequest
            {
                QueueUrl = destinationQueue.Url,
                Entries = messages.Select(x => new SendMessageBatchRequestEntry { Id = x.MessageId, MessageBody = x.Body }).ToList()
            });

            var deleteResponse = sourceQueue.Client.DeleteMessageBatch(new DeleteMessageBatchRequest
            {
                QueueUrl = sourceQueue.Url,
                Entries = sendResponse.Successful.Select(x => new DeleteMessageBatchRequestEntry
                {
                    Id = x.Id,
                    ReceiptHandle = receiptHandles[x.Id]
                }).ToList()
            });

            Console.WriteLine("Moved {0} messages from {1} to {2}", sendResponse.Successful.Count, SourceQueueName, DestinationQueueName);
            return true;
        }

        private List<Message> PopMessagesFromSourceQueue(SqsQueueByName sourceQueue)
        {
            var messages = new List<Message>();
            ReceiveMessageResponse receiveResponse;
            do
            {
                receiveResponse = sourceQueue.Client.ReceiveMessage(new ReceiveMessageRequest
                {
                    QueueUrl = sourceQueue.Url,
                    MaxNumberOfMessages = Count,
                });
                messages.AddRange(receiveResponse.Messages);
            } while (messages.Count < Count && receiveResponse.Messages.Any());

            
            return messages;
        }
    }
}