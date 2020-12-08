using System;
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
        public HandleMessageContext(Message message, Type messageType, string queueName)
        {
            Message = message;
            MessageType = messageType;
            QueueName = queueName;
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
    }
}
