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
    /// <param name="destinationType">The type of destination (Topic or Queue) where the message will be published.</param>
    /// <returns>A <see cref="PublishMessage"/> object containing the converted message body, attributes, and any additional publishing information.</returns>
    /// <remarks>
    /// This method handles the following operations:
    /// - Serializes the message body
    /// - Adds custom message attributes
    /// - Applies compression to the message body if it meets specified criteria
    /// - Adds compression-related attributes if compression is applied
    /// - Prepares the message for SNS (if applicable) by setting the subject
    /// The exact behavior may vary based on the destination type and compression options.
    /// </remarks>
    PublishMessage ConvertForPublish(Message message, PublishMetadata publishMetadata, PublishDestinationType destinationType);
}
