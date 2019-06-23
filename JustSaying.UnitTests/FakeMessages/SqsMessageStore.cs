using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Security.Cryptography;
using System.Text;
using System.Xml.Serialization;
using JustEat.HttpClientInterception;
using JustSaying.Models;
using JustSaying.UnitTests.FakeMessages.Xml;
using Newtonsoft.Json;

namespace JustSaying.UnitTests.FakeMessages
{
    public sealed class SqsMessageStore
    {
        private static readonly JsonSerializerSettings _jsonSettings = new JsonSerializerSettings();

        private readonly ConcurrentQueue<MessageEnvelope> _messages = new ConcurrentQueue<MessageEnvelope>();
        private readonly HttpClientInterceptorOptions _options;

        public SqsMessageStore(string queueName, string regionName, HttpClientInterceptorOptions options)
        {
            QueueName = queueName ?? throw new ArgumentNullException(nameof(queueName));
            RegionName = regionName ?? throw new ArgumentNullException(nameof(regionName));
            _options = options ?? throw new ArgumentNullException(nameof(options));

            QueueUri = new Uri($"https://sqs.{RegionName}.amazonaws.com/123456789012/{QueueName}");

            RegisterQueue();
            RegisterGetQueueAttributes();
            RegisterSetQueueAttributes();
            RegisterDeleteMessage();
            RegisterMessages();
        }

        public string QueueName { get; }

        public Uri QueueUri { get; }

        public string RegionName { get; set; }

        public SqsMessageStore Add(Message message)
        {
            if (message == null)
            {
                throw new ArgumentNullException(nameof(message));
            }

            string subject = message.GetType().Name;

            var payload = new
            {
                Subject = subject,
                Message = message,
            };

            string body = JsonConvert.SerializeObject(payload, _jsonSettings);
            string hash = GetBodyMD5(body);

            var envelope = new MessageEnvelope()
            {
                Body = body,
                Hash = hash,
                Timestamp = DateTimeOffset.UtcNow,
            };

            _messages.Enqueue(envelope);

            return this;
        }

        private byte[] GetMessages()
        {
            int messagesToReceive = 10;
            var messages = new List<MessageEnvelope>();

            for (int i = 0; i < messagesToReceive; i++)
            {
                if (_messages.TryDequeue(out var message))
                {
                    messages.Add(message);
                }
            }

            var response = new SqsReceiveMessageResponse()
            {
                ReceiveMessageResult = new SqsReceiveMessageResult()
                {
                    Messages = new List<SqsMessage>(),
                },
                ResponseMetadata = new SqsResponseMetadata(),
            };

            foreach (var message in messages)
            {
                response.ReceiveMessageResult.Messages.Add(new SqsMessage()
                {
                    MD5OfBody = message.Hash,
                    Body = message.Body,
                    Attributes = new List<SqsAttribute>(),
                    MessageId = Guid.NewGuid().ToString(),
                    ReceiptHandle = Guid.NewGuid().ToString(),
                });
            }

            return ToXml(response);
        }

        private static string GetBodyMD5(string body)
        {
            byte[] hashBytes;

            using (var algorithm = HashAlgorithm.Create("MD5"))
            {
                hashBytes = algorithm.ComputeHash(Encoding.UTF8.GetBytes(body));
            }

            var hash = new StringBuilder();

            foreach (byte b in hashBytes)
            {
                hash.Append(b.ToString("x2", CultureInfo.InvariantCulture));
            }

            return hash.ToString();
        }

        private void RegisterQueue()
        {
            byte[] GetXml()
            {
                var result = new SqsGetQueueUrlResponse()
                {
                    GetQueueUrlResult = new SqsGetQueueUrlResult()
                    {
                        QueueUrl = QueueUri.ToString(),
                    },
                    ResponseMetadata = new SqsResponseMetadata(),
                };

                return ToXml(result);
            }

            new HttpRequestInterceptionBuilder()
                .Requests()
                .ForPost()
                .ForUrl($"https://sqs.{RegionName}.amazonaws.com")
                .ForFormContent(new Dictionary<string, string>() { ["Action"] = "GetQueueUrl" })
                .Responds()
                .WithContent(GetXml)
                .RegisterWith(_options);
        }

        private void RegisterGetQueueAttributes()
        {
            byte[] GetXml()
            {
                var queueAttributes = new SqsGetQueueAttributesResponse()
                {
                    GetQueueAttributesResult = new SqsGetQueueAttributesResult()
                    {
                        Attributes = new List<SqsAttribute>()
                        {
                            new SqsAttribute() { Name = "ApproximateNumberOfMessages", Value = _messages.Count.ToString(CultureInfo.InvariantCulture) },
                            new SqsAttribute() { Name = "ApproximateNumberOfMessagesDelayed", Value = "0" },
                            new SqsAttribute() { Name = "ApproximateNumberOfMessagesNotVisible", Value = "0" },
                            new SqsAttribute() { Name = "CreatedTimestamp", Value = DateTimeOffset.UtcNow.ToUnixTimeSeconds().ToString(CultureInfo.InvariantCulture) },
                            new SqsAttribute() { Name = "DelaySeconds", Value = "0" },
                            new SqsAttribute() { Name = "LastModifiedTimestamp", Value = DateTimeOffset.UtcNow.ToUnixTimeSeconds().ToString(CultureInfo.InvariantCulture) },
                            new SqsAttribute() { Name = "MaximumMessageSize", Value = "8192" },
                            new SqsAttribute() { Name = "MessageRetentionPeriod", Value = "345600" },
                            new SqsAttribute() { Name = "QueueArn", Value = $"arn:aws:sqs:{RegionName}:123456789012:{QueueName}" },
                            new SqsAttribute() { Name = "ReceiveMessageWaitTimeSeconds", Value = "2" },
                            new SqsAttribute() { Name = "VisibilityTimeout", Value = "30" },
                        },
                    },
                    ResponseMetadata = new SqsResponseMetadata(),
                };

                return ToXml(queueAttributes);
            }

            RegisterQueueAction("GetQueueAttributes", GetXml);
        }

        private void RegisterSetQueueAttributes()
        {
            byte[] GetXml()
            {
                return ToXml(new SqsSetQueueAttributesResponse());
            }

            RegisterQueueAction("SetQueueAttributes", GetXml);
        }

        private void RegisterMessages()
        {
            RegisterQueueAction("ReceiveMessage", GetMessages);
        }

        private void RegisterDeleteMessage()
        {
            byte[] GetXml()
            {
                return ToXml(new SqsDeleteMessageResponse());
            }

            RegisterQueueAction("DeleteMessage", GetXml);
        }

        private void RegisterQueueAction(string actionName, Func<byte[]> contentFactory)
        {
            new HttpRequestInterceptionBuilder()
                .Requests()
                .ForPost()
                .ForUri(QueueUri)
                .ForFormContent(new Dictionary<string, string>() { ["Action"] = actionName })
                .Responds()
                .WithContent(contentFactory)
                .RegisterWith(_options);
        }

        private static byte[] ToXml<T>(T value)
        {
            var xmlSerializer = new XmlSerializer(typeof(T));

            using (var stream = new MemoryStream())
            {
                xmlSerializer.Serialize(stream, value);
                return stream.ToArray();
            }
        }

        private sealed class MessageEnvelope
        {
            public DateTimeOffset Timestamp { get; set; }

            public string Body { get; set; }

            public string Hash { get; set; }
        }
    }
}
