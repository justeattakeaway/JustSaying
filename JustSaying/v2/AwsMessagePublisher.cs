using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Threading.Tasks;
using Amazon;
using Amazon.SimpleNotificationService;
using Amazon.SQS;
using JustSaying.AwsTools;
using JustSaying.AwsTools.MessageHandling;
using JustSaying.AwsTools.QueueCreation;
using JustSaying.Extensions;
using JustSaying.Messaging;
using JustSaying.Messaging.MessageSerialisation;
using JustSaying.Messaging.Monitoring;
using JustSaying.Models;
using JustSaying.v2.Configuration;
using Microsoft.Extensions.Logging;

namespace JustSaying.v2
{
    public interface IAwsMessagePublisher
    {
        void Add<TMessage>(AwsTopicPublisherConfiguration configuration) where TMessage : Message;
        void Add<TMessage>(AwsQueuePublisherConfiguration configuration) where TMessage : Message;
        Task<IMessagePublisher> BuildAsync();
    }

    public class AwsMessagePublisher : DeferredActionBuilder, IAwsMessagePublisher, IMessagePublisher
    {
        private readonly Dictionary<string, Dictionary<string, IMessagePublisher>> _publishersByRegionAndTopic = new Dictionary<string, Dictionary<string, IMessagePublisher>>();

        private readonly int _publishFailureReAttempts;
        private readonly int _publishFailureBackoffMilliseconds;
        private readonly IAwsRegionProvider _regionProvider;
        private readonly IAwsClientFactoryProxy _awsClientFactoryProxy;
        private readonly IAwsNamingStrategy _namingStrategy;
        private readonly IMessageSerialisationRegister _messageSerialisationRegister;
        private readonly IMessageSerialisationFactory _messageSerialisationFactory;
        private readonly IMessageMonitor _messageMonitor;
        private readonly ILoggerFactory _loggerFactory;
        private readonly ILogger _logger;

        public AwsMessagePublisher(int publisherFailureReAttempts, int publishFailureBackoffMilliseconds, IAwsRegionProvider regionProvider, IAwsClientFactoryProxy awsClientFactoryProxy, IAwsNamingStrategy namingStrategy, IMessageSerialisationRegister messageSerialisationRegister, IMessageSerialisationFactory messageSerialisationFactory, IMessageMonitor messageMonitor, ILoggerFactory loggerFactory)
        {
            _publishFailureReAttempts = publisherFailureReAttempts;
            _publishFailureBackoffMilliseconds = publishFailureBackoffMilliseconds;
            _regionProvider = regionProvider;
            _awsClientFactoryProxy = awsClientFactoryProxy;
            _namingStrategy = namingStrategy;
            _messageSerialisationRegister = messageSerialisationRegister;
            _messageSerialisationFactory = messageSerialisationFactory;
            _messageMonitor = messageMonitor;
            _loggerFactory = loggerFactory;
            _logger = loggerFactory.CreateLogger("JustSaying");

            if (publisherFailureReAttempts == 0)
            {
                _logger.LogWarning("You have not set a re-attempt value for publish failures. If the publish location is 'down' you may lose messages!");
            }
        }

        public async Task PublishAsync(Message message)
        {
            var publisher = GetActivePublisherForMessageAsync(message);
            await PublishMessageAsync(publisher, message);
        }
#if AWS_SDK_HAS_SYNC
        public void Publish(Message message) => PublishAsync(message).GetAwaiter().GetResult();
#endif
        public void Add<TMessage>(AwsTopicPublisherConfiguration configuration) where TMessage : Message => QueueAction(async () => await AddTopicPublisher<TMessage>(configuration));
        public void Add<TMessage>(AwsQueuePublisherConfiguration configuration) where TMessage : Message => QueueAction(async () => await AddQueuePublisher<TMessage>(configuration));

        public async Task<IMessagePublisher> BuildAsync()
        {
            await ExecuteActionsAsync();
            return this;
        }

        private IMessagePublisher GetActivePublisherForMessageAsync(Message message)
        {
            var activeRegion = _regionProvider.GetActiveRegion == null ? _regionProvider.AvailableRegions.First() : _regionProvider.GetActiveRegion();

            _logger.LogInformation($"Active region has been evaluated to {activeRegion}");

            if (!_publishersByRegionAndTopic.ContainsKey(activeRegion))
            {
                _logger.LogError($"Error publishing message, no publishers registered for region {activeRegion}.");
                throw new InvalidOperationException($"Error publishing message, no publishers registered for region {activeRegion}.");
            }

            var topic = message.GetType().ToTopicName();
            var publishersByTopic = _publishersByRegionAndTopic[activeRegion];

            if (publishersByTopic.ContainsKey(topic))
            {
                return publishersByTopic[topic];
            }

            _logger.LogError($"Error publishing message, no publishers registered for message type {message} in {activeRegion}.");
            throw new InvalidOperationException($"Error publishing message, no publishers registered for message type {message} in {activeRegion}.");
        }
        private IAmazonSQS GetSqsClientForRegion(RegionEndpoint regionEndpoint) => _awsClientFactoryProxy.GetAwsClientFactory().GetSqsClient(regionEndpoint);
        private IAmazonSimpleNotificationService GetSnsClientForRegion(RegionEndpoint regionEndpoint) => _awsClientFactoryProxy.GetAwsClientFactory().GetSnsClient(regionEndpoint);
        private async Task AddTopicPublisher<TMessage>(AwsTopicPublisherConfiguration topicPublisherConfiguration) where TMessage : Message
        {
            _logger.LogInformation("Adding SNS publisher");

            RegisterMessageSerialiser<TMessage>();

            var additionalSubscriberAccounts = (topicPublisherConfiguration.AdditionalSubscribers ?? new List<string>()).Where(item => !string.IsNullOrEmpty(item)).ToList();
            var messageType = typeof(TMessage).ToTopicName();
            var topicName = _namingStrategy.GetTopicName<TMessage>(topicPublisherConfiguration);

            foreach (var region in _regionProvider.AvailableRegions)
            {
                var eventPublisher = new SnsTopicByName(topicName, GetSnsClientForRegion(RegionEndpoint.GetBySystemName(region)), _messageSerialisationRegister, _loggerFactory);

                if (!await eventPublisher.ExistsAsync())
                {
                    await eventPublisher.CreateAsync();
                }

                await eventPublisher.EnsurePolicyIsUpdatedAsync(additionalSubscriberAccounts);
                CacheMessagePublisher(messageType, region, eventPublisher);
            }

            _logger.LogInformation($"Created SNS topic publisher - Topic: {messageType}");
        }
        private async Task AddQueuePublisher<TMessage>(AwsQueuePublisherConfiguration queuePublisherConfiguration) where TMessage : Message
        {
            _logger.LogInformation("Adding SQS publisher");

            RegisterMessageSerialiser<TMessage>();

            var sqsConfig = new SqsBasicConfiguration
            {
                ErrorQueueRetentionPeriodSeconds = queuePublisherConfiguration.ErrorQueueRetentionPeriodSeconds,
                MessageRetentionSeconds = queuePublisherConfiguration.MessageRetentionSeconds,
                VisibilityTimeoutSeconds = queuePublisherConfiguration.VisibilityTimeoutSeconds,
                DeliveryDelaySeconds = queuePublisherConfiguration.DeliveryDelaySeconds,
                ErrorQueueOptOut = queuePublisherConfiguration.ErrorQueueOptOut
            };

            var messageType = typeof(TMessage).ToTopicName();
            var queueName = _namingStrategy.GetQueueName<TMessage>(queuePublisherConfiguration, true);

            foreach (var region in _regionProvider.AvailableRegions)
            {
                var regionEndpoint = RegionEndpoint.GetBySystemName(region);
                var eventPublisher = new SqsPublisher(regionEndpoint, queueName, GetSqsClientForRegion(regionEndpoint), queuePublisherConfiguration.RetryCountBeforeSendingToErrorQueue, _messageSerialisationRegister, _loggerFactory);

                if (!await eventPublisher.ExistsAsync())
                {
                    await eventPublisher.CreateAsync(sqsConfig);
                }

                CacheMessagePublisher(messageType, region, eventPublisher);
            }

            _logger.LogInformation($"Created SQS publisher - MessageName: {messageType}, QueueName: {queueName}");
        }
        private async Task PublishMessageAsync(IMessagePublisher publisher, Message message, int attemptCount = 0)
        {
            attemptCount++;
            try
            {
                var watch = Stopwatch.StartNew();

                await publisher.PublishAsync(message);

                watch.Stop();
                _messageMonitor.PublishMessageTime(watch.ElapsedMilliseconds);
            }
            catch (Exception ex)
            {
                if (attemptCount >= _publishFailureReAttempts)
                {
                    _messageMonitor.IssuePublishingMessage();
                    _logger.LogError(0, ex, $"Failed to publish message {message.GetType().Name}. Halting after attempt {attemptCount}");
                    throw;
                }

                _logger.LogWarning(0, ex, $"Failed to publish message {message.GetType().Name}. Retrying after attempt {attemptCount} of {_publishFailureReAttempts}");
                await Task.Delay(_publishFailureBackoffMilliseconds * attemptCount);
                await PublishMessageAsync(publisher, message, attemptCount);
            }
        }
        private void RegisterMessageSerialiser<T>() where T : Message => _messageSerialisationRegister.AddSerialiser<T>(_messageSerialisationFactory.GetSerialiser<T>());
        private void CacheMessagePublisher(string messageType, string region, IMessagePublisher messagePublisher)
        {
            if (!_publishersByRegionAndTopic.TryGetValue(region, out var publishersByTopic))
            {
                publishersByTopic = new Dictionary<string, IMessagePublisher>();
                _publishersByRegionAndTopic.Add(region, publishersByTopic);
            }

            publishersByTopic[messageType] = messagePublisher;
        }
    }
}