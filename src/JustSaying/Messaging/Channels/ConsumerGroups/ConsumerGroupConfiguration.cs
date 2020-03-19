using System;
using System.Collections.Generic;

namespace JustSaying.Messaging.Channels.ConsumerGroups
{
    public class ConsumerGroupConfiguration
    {
        private const string DefaultConcurrencyGroup = "Default";

        private readonly Dictionary<string, string> _queueNameToGroupName;
        private readonly Dictionary<string, ConsumerGroupSettings> _groupNameToConcurrencyLimit;

        public ConsumerGroupConfiguration(
            int defaultConsumerCount,
            int defaultBufferSize,
            int defaultMultiplexerCapacity,
            int defaultPrefetch)
        {
            _queueNameToGroupName = new Dictionary<string, string>();
            _groupNameToConcurrencyLimit = new Dictionary<string, ConsumerGroupSettings>();

            _defaultSettings = () => new ConsumerGroupSettings(
                defaultConsumerCount,
                defaultBufferSize,
                defaultMultiplexerCapacity,
                defaultPrefetch);

            _groupNameToConcurrencyLimit.Add(DefaultConcurrencyGroup, _defaultSettings());
        }

        private readonly Func<ConsumerGroupSettings> _defaultSettings;

        public void EnsureConsumerGroupExists(string groupName, Action<ConsumerGroupSettings> groupSettings)
        {
            if (_groupNameToConcurrencyLimit.ContainsKey(groupName)) return;

            var newSettings = _defaultSettings();
            groupSettings(newSettings);
            _groupNameToConcurrencyLimit.Add(groupName, newSettings);
        }

        public void SetConsumerGroup(string queueName, string groupName)
        {
            if (!_groupNameToConcurrencyLimit.ContainsKey(groupName))
            {
                throw new InvalidOperationException(
                    $"A group with name \"{groupName}\" must first be added with EnsureConcurrencyGroupExists");
            }

            _queueNameToGroupName.Add(queueName, groupName);
        }

        public ConsumerGroupSettings GetConsumerGroup(string group) =>
            _groupNameToConcurrencyLimit[group];

        public string GetConsumerGroupForQueue(string queueName)
        {
            return _queueNameToGroupName.ContainsKey(queueName)
                ? _queueNameToGroupName[queueName]
                : DefaultConcurrencyGroup;
        }

        public IEnumerable<string> GetAllConcurrencyGroups() => _groupNameToConcurrencyLimit.Keys;
    }
}
