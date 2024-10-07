using JustSaying.AwsTools.MessageHandling;
using JustSaying.Messaging.MessageSerialization;
using JustSaying.Models;

namespace JustSaying.Messaging;

public interface IPublishMessageConverter
{
    /// <summary>
    /// Converts a message to a format suitable for publishing, applying necessary transformations and compression.
    /// </summary>
    /// <param name="message">The original message to be converted.</param>
    /// <param name="publishMetadata">Metadata associated with the publish operation, including any custom message attributes.</param>
    /// <returns>A <see cref="PublishMessage"/> object containing the converted message body, attributes, and any additional publishing information.</returns>
    /// <remarks>
    /// This method handles the following operations:
    /// <ul>
    /// <li>Serializes the message body</li>
    /// <li>Adds custom message attributes</li>
    /// <li>Applies compression to the message body if it meets specified criteria</li>
    /// <li>Adds compression-related attributes if compression is applied</li>
    /// <li>Prepares the message for SNS (if applicable) by setting the subject</li>
    /// </ul>
    /// The exact behavior may vary based on the destination type and compression options.
    /// </remarks>
    ValueTask<PublishMessage> ConvertForPublishAsync(Message message, PublishMetadata publishMetadata);
}
