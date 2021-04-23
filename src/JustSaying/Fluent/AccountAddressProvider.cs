using System;
using Amazon;
using JustSaying.Naming;

namespace JustSaying.Fluent
{
    /// <summary>
    /// Creates resource addresses either by name or naming convention.
    /// </summary>
    public sealed class AccountAddressProvider
    {
        private readonly string _accountId;
        private readonly RegionEndpoint _regionEndpoint;
        private readonly IQueueNamingConvention _queueNamingConvention;
        private readonly ITopicNamingConvention _topicNamingConvention;

        /// <summary>
        /// Initializes a new instance of the <see cref="AccountAddressProvider"/> class with the default naming convention.
        /// </summary>
        /// <param name="accountId">The AWS account ID the topics and queues belong in.</param>
        /// <param name="regionName">The AWS region the topics and queues belong in.</param>
        public AccountAddressProvider(string accountId, string regionName)
            : this(accountId, RegionEndpoint.GetBySystemName(regionName))
        { }

        /// <summary>
        /// Initializes a new instance of the <see cref="AccountAddressProvider"/> class with the default naming convention.
        /// </summary>
        /// <param name="accountId">The AWS account ID the topics and queues belong in.</param>
        /// <param name="regionEndpoint">The AWS region the topics and queues belong in.</param>
        public AccountAddressProvider(string accountId, RegionEndpoint regionEndpoint)
        {
            _accountId = accountId;
            _regionEndpoint = regionEndpoint;
            var namingConventions = new DefaultNamingConventions();
            _queueNamingConvention = namingConventions;
            _topicNamingConvention = namingConventions;
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="AccountAddressProvider"/> class with provided naming conventions.
        /// </summary>
        /// <param name="accountId">The AWS account ID the topics and queues belong in.</param>
        /// <param name="regionName">The AWS region the topics and queues belong in.</param>
        /// <param name="queueNamingConvention">A <see cref="IQueueNamingConvention"/> to use for producing a queue name from a message type.</param>
        /// <param name="topicNamingConvention">A <see cref="ITopicNamingConvention"/> to use for producing a topic name from a message type</param>
        public AccountAddressProvider(string accountId, string regionName, IQueueNamingConvention queueNamingConvention, ITopicNamingConvention topicNamingConvention)
            : this(accountId, RegionEndpoint.GetBySystemName(regionName), queueNamingConvention, topicNamingConvention)
        { }

        /// <summary>
        /// Initializes a new instance of the <see cref="AccountAddressProvider"/> class with provided naming conventions.
        /// </summary>
        /// <param name="accountId">The AWS account ID the topics and queues belong in.</param>
        /// <param name="regionEndpoint">The AWS region the topics and queues belong in.</param>
        /// <param name="queueNamingConvention">A <see cref="IQueueNamingConvention"/> to use for producing a queue name from a message type.</param>
        /// <param name="topicNamingConvention">A <see cref="ITopicNamingConvention"/> to use for producing a topic name from a message type</param>
        public AccountAddressProvider(string accountId, RegionEndpoint regionEndpoint, IQueueNamingConvention queueNamingConvention, ITopicNamingConvention topicNamingConvention)
        {
            _accountId = accountId;
            _regionEndpoint = regionEndpoint;
            _queueNamingConvention = queueNamingConvention;
            _topicNamingConvention = topicNamingConvention;
        }

        /// <summary>
        /// Creates a <see cref="TopicAddress"/> within the current account from <see cref="T"/> by using the topic naming convention.
        /// </summary>
        /// <typeparam name="T">Message type</typeparam>
        /// <returns>The <see cref="TopicAddress"/> for this message type.</returns>
        public TopicAddress GetTopicAddressByConvention<T>()
        {
            return GetTopicAddress(_topicNamingConvention.TopicName<T>());
        }

        /// <summary>
        /// Creates a <see cref="TopicAddress"/> in the current account with name specified with <see cref="topicName"/>.
        /// </summary>
        /// <param name="topicName">The topic name.</param>
        /// <returns>The <see cref="TopicAddress"/> for this topic.</returns>
        public TopicAddress GetTopicAddress(string topicName)
        {
            return new TopicAddress
            {
                TopicArn = new Arn
                {
                    Partition = _regionEndpoint.PartitionName,
                    Service = "sns",
                    Region = _regionEndpoint.SystemName,
                    AccountId = _accountId,
                    Resource = topicName
                }.ToString()
            };
        }

        /// <summary>
        /// Creates a <see cref="QueueAddress"/> within the current account from <see cref="T"/> by using the queue naming convention.
        /// </summary>
        /// <typeparam name="T">Message type</typeparam>
        /// <returns>The <see cref="QueueAddress"/> for this message type.</returns>
        public QueueAddress GetQueueAddressByConvention<T>()
        {
            return GetQueueAddress(_queueNamingConvention.QueueName<T>());
        }

        /// <summary>
        /// Creates a <see cref="QueueAddress"/> in the current account with name specified with <see cref="queueName"/>.
        /// </summary>
        /// <param name="queueName">The queue name.</param>
        /// <returns>The <see cref="QueueAddress"/> for this queue.</returns>
        public QueueAddress GetQueueAddress(string queueName)
        {
            var hostname = _regionEndpoint.GetEndpointForService("sqs").Hostname;
            Uri queueUrl = new UriBuilder("https", hostname)
            {
                Path = $"{_accountId}/{queueName}"
            }.Uri;

            return new QueueAddress
            {
                QueueUrl = queueUrl,
                RegionName = _regionEndpoint.SystemName
            };
        }
    }
}
