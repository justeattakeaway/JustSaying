using JustSaying.Messaging.Channels.Context;
using JustSaying.Messaging.MessageHandling;
using JustSaying.Models;


// ReSharper disable once CheckNamespace
namespace JustSaying.Messaging.Middleware;

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
    public HandleMessageContext(string queueName, Amazon.SQS.Model.Message rawMessage, Message message,
        Type messageType, IMessageVisibilityUpdater visibilityUpdater, IMessageDeleter messageDeleter,
        Uri queueUri, MessageAttributes messageAttributes)
    {
        Message = message;
        MessageType = messageType;
        QueueName = queueName;
        VisibilityUpdater = visibilityUpdater;
        MessageDeleter = messageDeleter;
        QueueUri = queueUri;
        MessageAttributes = messageAttributes;
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

    /// <summary>
    /// The Absolute Uri of the queue this message came from.
    /// </summary>
    public Uri QueueUri { get; }

    /// <summary>
    /// A <see cref="MessageAttributes"/> collection of attributes that were downloaded with this message.
    /// </summary>
    public MessageAttributes MessageAttributes { get; }

    /// <summary>
    /// The raw SQS message that was downloaded from the queue.
    /// </summary>
    public Amazon.SQS.Model.Message RawMessage { get; }

    /// <summary>
    /// An <see cref="IMessageVisibilityUpdater"/> that can be used to update the visibility timeout for this message.
    /// </summary>
    public IMessageVisibilityUpdater VisibilityUpdater { get; }

    /// <summary>
    /// An <see cref="IMessageDeleter"/> that can be used to delete this message.
    /// </summary>
    public IMessageDeleter MessageDeleter { get; }

    /// <summary>
    /// The <see cref="Exception"/> that occurred in the handling of this message.
    /// </summary>
    public Exception HandledException { get; private set; }

    /// <summary>
    /// Sets an <see cref="Exception"/> to be available to other middlewares.
    /// </summary>
    /// <param name="e">The <see cref="Exception"/> to set for this context.</param>
    public void SetException(Exception e)
    {
        HandledException = e;
    }
}