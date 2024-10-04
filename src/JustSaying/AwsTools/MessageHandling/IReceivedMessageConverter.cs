using JustSaying.Messaging.MessageSerialization;

namespace JustSaying.Messaging;

public interface IReceivedMessageConverter
{
    /// <summary>
    /// Converts an Amazon SQS message to a <see cref="ReceivedMessage" /> object.
    /// </summary>
    /// <param name="message">The Amazon SQS message to convert.</param>
    /// <returns>A <see cref="ReceivedMessage" /> object containing the deserialized message body and attributes.</returns>
    /// <remarks>
    /// This method handles the conversion of both raw SQS messages and SNS-wrapped messages.
    /// It also applies any necessary decompression to the message body.
    /// </remarks>
    ReceivedMessage ConvertForReceive(Amazon.SQS.Model.Message message);
}
