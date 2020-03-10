using System;
using System.Collections.Generic;
using System.Threading;
using JustSaying.AwsTools.MessageHandling;
using JustSaying.Messaging;
using JustSaying.Messaging.MessageHandling;
using JustSaying.Messaging.MessageSerialization;
using JustSaying.Messaging.Monitoring;
using JustSaying.Messaging.Policies;
using JustSaying.Models;

namespace JustSaying
{
    public interface IAmJustSaying : IMessagePublisher
    {
        void AddQueue(ISqsQueue queue);

        void AddMessageHandler<T>(Func<IHandlerAsync<T>> handler) where T : Message;

        void AddMessagePublisher<T>(IMessagePublisher messagePublisher) where T : Message;

        void Start(CancellationToken cancellationToken = default);

        IMessagingConfig Config { get; }

        IMessageMonitor Monitor { get; set; }

        IMessageSerializationRegister SerializationRegister { get; }

        IMessageLockAsync MessageLock { get; set; }

        IMessageContextAccessor MessageContextAccessor { get; set; }
    }
}
