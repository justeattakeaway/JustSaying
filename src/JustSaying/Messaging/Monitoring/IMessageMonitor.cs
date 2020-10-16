using System;
using Amazon.SQS.Model;

namespace JustSaying.Messaging.Monitoring
{
    /// <summary>
    /// Provides a mechanism for monitoring when various message actions are
    /// performed in JustSaying. This is typically used for metrics gathering.
    /// </summary>
    public interface IMessageMonitor
    {
        /// <summary>
        /// Monitors when a specific message type threw an exception while being handled
        /// by a subscriber.
        /// </summary>
        /// <param name="messageType">The type of the message that was being handled.</param>
        void HandleException(Type messageType);

        /// <summary>
        /// Monitors when a particular message that threw an exception while being handled
        /// by a subscriber.
        /// </summary>
        /// <param name="ex">The exception that was thrown.</param>
        /// <param name="message">The message being handled when the exception was thrown.</param>
        void HandleError(Exception ex, Message message);

        /// <summary>
        /// Monitors the length of time a message took to be handled by a subscriber.
        /// </summary>
        /// <param name="duration">The length of time taken.</param>
        void HandleTime(TimeSpan duration);

        /// <summary>
        /// Monitors when a message was not successfully published after waiting for
        /// the <see cref="IPublishConfiguration.PublishFailureReAttempts">configured amount of retries</see>.
        /// </summary>
        void IssuePublishingMessage();

        /// <summary>
        /// Monitors when each message is handled by a subscriber.
        /// </summary>
        /// <param name="message">The message that was handled.</param>
        void Handled(JustSaying.Models.Message message);

        /// <summary>
        /// Monitors each time a message took over 1ms to be written to a ChannelWriter.
        /// </summary>
        void IncrementThrottlingStatistic();

        /// <summary>
        /// Monitors the length of time a message took to be written to a ChannelWriter,
        /// if that duration was longer than 1ms.
        /// </summary>
        /// <param name="duration">The length of time the write took.</param>
        void HandleThrottlingTime(TimeSpan duration);

        /// <summary>
        /// Monitors the length of time a message took to be published.
        /// </summary>
        /// <param name="duration">The length of time.</param>
        void PublishMessageTime(TimeSpan duration);

        /// <summary>
        /// Monitors the length of time a message's content took to be retrieved from SQS.
        /// </summary>
        /// <param name="duration">The length of time</param>
        /// <param name="queueName">The name of the queue the message was retrieved from.</param>
        /// <param name="region">The region the SQS queue belonged to.</param>
        void ReceiveMessageTime(TimeSpan duration, string queueName, string region);
    }
}
