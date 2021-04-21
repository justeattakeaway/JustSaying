using System;
using Amazon;
using JustSaying.Naming;

namespace JustSaying.Fluent
{
    /// <summary>
    ///
    /// </summary>
    public sealed class AccountAddressProvider
    {
        private readonly string _accountId;
        private readonly RegionEndpoint _regionEndpoint;
        private readonly IQueueNamingConvention _queueNamingConvention;
        private readonly ITopicNamingConvention _topicNamingConvention;

        /// <summary>
        ///
        /// </summary>
        /// <param name="accountId"></param>
        /// <param name="regionName"></param>
        public AccountAddressProvider(string accountId, string regionName)
        {
            _accountId = accountId;
            _regionEndpoint = RegionEndpoint.GetBySystemName(regionName);
        }

        /// <summary>
        ///
        /// </summary>
        /// <param name="accountId"></param>
        /// <param name="regionName"></param>
        /// <param name="queueNamingConvention"></param>
        /// <param name="topicNamingConvention"></param>
        public AccountAddressProvider(string accountId, string regionName, IQueueNamingConvention queueNamingConvention, ITopicNamingConvention topicNamingConvention)
        {
            _accountId = accountId;
            _regionEndpoint = RegionEndpoint.GetBySystemName(regionName);
            _queueNamingConvention = queueNamingConvention;
            _topicNamingConvention = topicNamingConvention;
        }

        /// <summary>
        ///
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <returns></returns>
        public TopicAddress GetTopicAddressByConvention<T>()
        {
            return GetTopicAddress(_topicNamingConvention.TopicName<T>());
        }

        /// <summary>
        ///
        /// </summary>
        /// <param name="topicName"></param>
        /// <returns></returns>
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
        ///
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <returns></returns>
        public QueueAddress GetQueueAddressByConvention<T>()
        {
            return GetQueueAddress(_queueNamingConvention.QueueName<T>());
        }

        /// <summary>
        ///
        /// </summary>
        /// <param name="queueName"></param>
        /// <returns></returns>
        public QueueAddress GetQueueAddress(string queueName)
        {
            return new QueueAddress
            {
                QueueUrl = new Uri($"https://sqs.{_regionEndpoint.SystemName}.{_regionEndpoint.PartitionDnsSuffix}/{_accountId}/{queueName}"),
                RegionName = _regionEndpoint.SystemName
            };
        }
    }
}
