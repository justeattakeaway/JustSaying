using JustSaying.Messaging.MessageSerialization;

namespace JustSaying.Messaging;

public interface IInboundMessageConverter
{
    /// <summary>
    /// Converts an Amazon SQS message to a <see cref="InboundMessage" /> object.
    /// </summary>
    /// <param name="message">The Amazon SQS message to convert.</param>
    /// <param name="cancellationToken">A token to monitor for cancellation requests.</param>
    /// <returns>A <see cref="InboundMessage" /> object containing the deserialized message body and attributes.</returns>
    /// <remarks>
    /// This method handles the conversion of both raw SQS messages and SNS-wrapped messages.
    /// It also applies any necessary decompression to the message body.
    /// </remarks>
    ValueTask<InboundMessage> ConvertToInboundMessageAsync(Amazon.SQS.Model.Message message, CancellationToken cancellationToken = default);
}
