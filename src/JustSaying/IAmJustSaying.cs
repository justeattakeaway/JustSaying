using System;
using System.Threading;
using JustSaying.AwsTools.MessageHandling;
using JustSaying.Messaging;
using JustSaying.Messaging.MessageHandling;
using JustSaying.Messaging.MessageProcessingStrategies;
using JustSaying.Messaging.MessageSerialization;
using JustSaying.Messaging.Monitoring;
using JustSaying.Models;
using SQSMessage = Amazon.SQS.Model.Message;

namespace JustSaying
{
    public interface IAmJustSaying : IMessagePublisher
    {
        void AddQueue(string region, ISqsQueue queue);

        void AddMessageHandler<T>(Func<IHandlerAsync<T>> handler) where T : Message;

        // TODO - swap params
        void AddMessagePublisher<T>(IMessagePublisher messagePublisher, string region) where T : Message;

        void Start(CancellationToken cancellationToken = default);

        IMessagingConfig Config { get; }

        IMessageMonitor Monitor { get; set; }

        IMessageSerializationRegister SerializationRegister { get; }

        IMessageLockAsync MessageLock { get; set; }

        IMessageContextAccessor MessageContextAccessor { get; set; }

        void SetMessageBackoffStrategy(IMessageBackoffStrategy value);

        void SetOnError(Action<Exception, SQSMessage> value);
    }
}
