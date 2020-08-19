using System;
using System.Collections.Generic;
using JustSaying.AwsTools.MessageHandling;
using JustSaying.Messaging;
using JustSaying.Messaging.Channels.SubscriptionGroups;
using JustSaying.Messaging.MessageHandling;
using JustSaying.Messaging.MessageProcessingStrategies;
using JustSaying.Messaging.MessageSerialization;
using JustSaying.Messaging.Monitoring;
using JustSaying.Models;

namespace JustSaying
{
    public interface IAmJustSaying : IStartable, IMessagePublisher
    {
        void AddQueue(string region, string subscriptionGroup, ISqsQueue queue);

        void AddMessageHandler<T>(string queue, Func<IHandlerAsync<T>> handler) where T : Message;

        // TODO - swap params
        void AddMessagePublisher<T>(IMessagePublisher messagePublisher, string region) where T : Message;

        void SetGroupSettings(
            SubscriptionGroupSettingsBuilder defaults,
            IDictionary<string, SubscriptionGroupConfigBuilder> settings);

        IMessagingConfig Config { get; }

        IMessageMonitor Monitor { get; set; }

        IMessageSerializationRegister SerializationRegister { get; }

        IMessageLockAsync MessageLock { get; set; }

        IMessageContextAccessor MessageContextAccessor { get; set; }

        IMessageBackoffStrategy MessageBackoffStrategy { get; set; }
    }
}
