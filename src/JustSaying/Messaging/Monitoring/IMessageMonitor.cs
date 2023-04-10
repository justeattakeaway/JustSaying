using Amazon.SQS.Model;
using JustSaying.Messaging.MessageHandling;

namespace JustSaying.Messaging.Monitoring;

/// <summary>
/// Provides a mechanism for monitoring when various message actions are
/// performed in JustSaying. This is typically used for metrics gathering.
/// </summary>
public interface IMessageMonitor
{
    /// <summary>
    /// Called when a specific message type threw an exception while being handled
    /// by a <see cref="IHandlerAsync{T}"/>.
    /// </summary>
    /// <param name="messageType">The type of the message that was being handled.</param>
    void HandleException(Type messageType);

    /// <summary>
    /// Called when an error occurs while attempting to de-serialize a message for a subscriber.
    /// </summary>
    /// <param name="ex">The exception that was thrown.</param>
    /// <param name="message">The <see cref="Amazon.SQS.Model.Message" /> being handled when the exception was thrown.</param>
    void HandleError(Exception ex, Message message);

    /// <summary>
    /// Called when a subscription handler has completed, with the time taken by the handler.
    /// </summary>
    /// <param name="duration">The length of time taken.</param>
    void HandleTime(TimeSpan duration);

    /// <summary>
    /// Called when a message was not successfully published after waiting for
    /// the <see cref="IPublishConfiguration.PublishFailureReAttempts">configured amount of retries</see>.
    /// </summary>
    void IssuePublishingMessage();

    /// <summary>
    /// Called when each message is handled by a subscriber.
    /// </summary>
    /// <param name="message">The message that was handled.</param>
    void Handled(object message);

    /// <summary>
    /// Called each time a message pipeline is full, and a subscriber must wait until
    /// there is room to continue downloading the message. This method is only called if the
    /// duration was longer than the internal timeout (currently 1ms).
    /// </summary>
    void IncrementThrottlingStatistic();

    /// <summary>
    /// Called each time a message pipeline is full, indicating the length of time the
    /// message took to be written to the pipeline. This method is only called if the
    /// duration was longer than the internal timeout (currently 1ms).
    /// </summary>
    /// <param name="duration">The length of time the message took to be written.</param>
    void HandleThrottlingTime(TimeSpan duration);

    /// <summary>
    /// Called when a message is published, indicating the length of time the message
    /// took to be published.
    /// </summary>
    /// <param name="duration">The length of time.</param>
    void PublishMessageTime(TimeSpan duration);

    /// <summary>
    /// Called when a message's content is downloaded, indicating the length of time it
    /// took for the content to be retrieved from SQS.
    /// </summary>
    /// <param name="duration">The length of time</param>
    /// <param name="queueName">The name of the queue the message was retrieved from.</param>
    /// <param name="region">The region the SQS queue belonged to.</param>
    void ReceiveMessageTime(TimeSpan duration, string queueName, string region);
    void HandlerExecutionTime(Type handlerType, Type messageType, TimeSpan duration);
}
