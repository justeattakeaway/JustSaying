using System;
using JustSaying.Messaging.Channels.Context;
using JustSaying.Models;


// ReSharper disable once CheckNamespace
namespace JustSaying.Messaging.Middleware
{
    public sealed class HandleMessageContext
    {
        /// <summary>
        /// This class encapsulates a messages context as it passes through a middleware pipeline.
        /// </summary>
        /// <param name="message">The JustSaying message that was deserialized from SQS.</param>
        /// <param name="messageType">The type of the JustSaying message contained in <see cref="Message"/>.</param>
        /// <param name="queueName">The queue from which this message was received.</param>
        /// <param name="visibilityUpdater">The <see cref="IMessageVisibilityUpdater"/> to use to update message visibilities on failure.</param>
        /// <param name="messageDeleter">The <see cref="IMessageDeleter"/> to use to remove a message from the queue on success.</param>
        public HandleMessageContext(string queueName, Amazon.SQS.Model.Message rawMessage, Message message, Type messageType, IMessageVisibilityUpdater visibilityUpdater, IMessageDeleter messageDeleter)
        {
            Message = message;
            MessageType = messageType;
            QueueName = queueName;
            VisibilityUpdater = visibilityUpdater;
            MessageDeleter = messageDeleter;
            RawMessage = rawMessage;
        }

        /// <summary>
        /// The queue name from which this message was received.
        /// </summary>
        public string QueueName { get; }

        /// <summary>
        /// The type of the JustSaying message contained in <see cref="Message"/>.
        /// </summary>
        public Type MessageType { get; }

        /// <summary>
        /// The JustSaying message that was deserialized from SQS.
        /// </summary>
        public Message Message { get; }

        public Amazon.SQS.Model.Message RawMessage { get; }

        public IMessageVisibilityUpdater VisibilityUpdater { get; }

        public IMessageDeleter MessageDeleter { get; }
    }
}
