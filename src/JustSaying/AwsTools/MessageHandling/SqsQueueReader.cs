using Amazon.SQS.Model;
using JustSaying.Messaging;
using JustSaying.Messaging.Channels.Context;

namespace JustSaying.AwsTools.MessageHandling;

internal class SqsQueueReader
{
    private readonly ISqsQueue _sqsQueue;
    private readonly IInboundMessageConverter _messageConverter;

    internal SqsQueueReader(ISqsQueue sqsQueue, IInboundMessageConverter messageConverter)
    {
        _sqsQueue = sqsQueue;
        _messageConverter = messageConverter;
    }

    internal string QueueName => _sqsQueue.QueueName;

    internal string RegionSystemName => _sqsQueue.RegionSystemName;

    internal Uri Uri => _sqsQueue.Uri;

    internal IQueueMessageContext ToMessageContext(Message message)
    {
        return new QueueMessageContext(message, this, _messageConverter);
    }

    internal async Task<IList<Message>> GetMessagesAsync(
        int maximumCount,
        TimeSpan waitTime,
        ICollection<string> requestMessageAttributeNames,
        CancellationToken cancellationToken)
    {
        var sqsMessageResponse =
            await _sqsQueue.ReceiveMessagesAsync(_sqsQueue.Uri.AbsoluteUri,
                maximumCount,
                (int)waitTime.TotalSeconds,
                requestMessageAttributeNames.ToList(),
                cancellationToken).ConfigureAwait(false);

        return sqsMessageResponse;
    }

    internal async Task DeleteMessageAsync(
        string receiptHandle,
        CancellationToken cancellationToken)
    {
        await _sqsQueue.DeleteMessageAsync(_sqsQueue.Uri.ToString(), receiptHandle, cancellationToken).ConfigureAwait(false);
    }

    internal async Task ChangeMessageVisibilityAsync(
        string receiptHandle,
        TimeSpan timeout,
        CancellationToken cancellationToken)
    {
        await _sqsQueue.ChangeMessageVisibilityAsync(
            _sqsQueue.Uri.ToString(),
            receiptHandle,
            (int)timeout.TotalSeconds,
            cancellationToken).ConfigureAwait(false);
    }
}
