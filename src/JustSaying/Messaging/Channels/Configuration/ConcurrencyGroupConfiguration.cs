using System;
using System.Collections.Generic;
using System.Linq;

namespace JustSaying.Messaging.Channels
{
    public class ConcurrencyGroupConfiguration
    {
        const string DefaultConcurrencyGroup = "Default";

        private readonly Dictionary<string, string> _queueNameToGroupName;
        private readonly Dictionary<string, int> _groupNameToConcurrencyLimit;

        public ConcurrencyGroupConfiguration(int defaultConsumerCount)
        {
            _queueNameToGroupName = new Dictionary<string, string>();
            _groupNameToConcurrencyLimit = new Dictionary<string, int>();

            EnsureConcurrencyGroupExists(DefaultConcurrencyGroup, defaultConsumerCount);
        }

        public void EnsureConcurrencyGroupExists(string groupName, int maxConcurrency)
        {
            if (_groupNameToConcurrencyLimit.ContainsKey(groupName)) return;

            _groupNameToConcurrencyLimit.Add(groupName, maxConcurrency);
        }

        public void SetConcurrencyGroup(string queueName, string groupName)
        {
            if (!_groupNameToConcurrencyLimit.ContainsKey(groupName))
            {
                throw new InvalidOperationException(
                    $"A group with name \"{groupName}\" must first be added with EnsureConcurrencyGroupExists");
            }

            _queueNameToGroupName.Add(queueName, groupName);
        }

        public int GetConcurrencyForGroup(string group) =>
            _groupNameToConcurrencyLimit[group];

        public string GetConcurrencyGroupForQueue(string queueName)
        {
            return _queueNameToGroupName.ContainsKey(queueName)
                ? _queueNameToGroupName[queueName]
                : DefaultConcurrencyGroup;
        }

        public IEnumerable<string> GetAllConcurrencyGroups() => _groupNameToConcurrencyLimit.Keys;
    }
}
