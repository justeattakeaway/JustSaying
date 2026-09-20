using System.Text;
using JustSaying.Messaging;

namespace JustSaying.AwsTools.MessageHandling;

/// <summary>
/// Calculates the payload sizes that SNS and SQS validate against a destination's maximum message size.
/// </summary>
/// <remarks>
/// Both services count the message body and every part of each message attribute (name, data type and value)
/// towards the limit. For batch requests the combined size of all entries is validated, not just each entry.
/// </remarks>
internal static class MessagePayloadSize
{
    /// <summary>
    /// Calculates the size, in bytes, of a message body, subject and attributes.
    /// </summary>
    /// <param name="body">The message body.</param>
    /// <param name="attributes">The message attributes, if any.</param>
    /// <param name="subject">The message subject, if any.</param>
    public static int Calculate(
        string body,
        IReadOnlyDictionary<string, MessageAttributeValue> attributes,
        string subject = null)
    {
        int size = 0;

        if (body is not null)
        {
            size += Encoding.UTF8.GetByteCount(body);
        }

        if (subject is not null)
        {
            size += Encoding.UTF8.GetByteCount(subject);
        }

        if (attributes is not null)
        {
            foreach (var attribute in attributes)
            {
                size += CalculateAttribute(attribute.Key, attribute.Value);
            }
        }

        return size;
    }

    /// <summary>
    /// Calculates the size, in bytes, that a single message attribute contributes to the payload.
    /// </summary>
    /// <param name="key">The attribute name.</param>
    /// <param name="value">The attribute value.</param>
    public static int CalculateAttribute(string key, MessageAttributeValue value)
    {
        int size = Encoding.UTF8.GetByteCount(key);

        if (value is null)
        {
            return size;
        }

        if (value.DataType is not null)
        {
            size += Encoding.UTF8.GetByteCount(value.DataType);
        }

        if (value.StringValue is not null)
        {
            size += Encoding.UTF8.GetByteCount(value.StringValue);
        }

        if (value.BinaryValue is not null)
        {
            size += value.BinaryValue.Count;
        }

        return size;
    }
}
