using System.Text;
using JustSaying.Messaging;

namespace JustSaying.AwsTools.MessageHandling;

/// <summary>
/// Calculates the payload sizes that SNS and SQS validate against a destination's maximum message size.
/// </summary>
/// <remarks>
/// Both services count the message body and every part of each message attribute (name, data type and value)
/// towards the limit, and nothing else: an SNS subject and a batch entry's ID are not counted. For batch requests
/// the combined size of all entries is validated, not just each entry.
/// </remarks>
internal static class MessagePayloadSize
{
    /// <summary>
    /// Calculates the size, in bytes, of a message body and attributes.
    /// </summary>
    /// <param name="body">The message body.</param>
    /// <param name="attributes">The message attributes, if any.</param>
    public static int Calculate(
        string body,
        IReadOnlyDictionary<string, MessageAttributeValue> attributes)
    {
        int size = 0;

        if (body is not null)
        {
            size += Encoding.UTF8.GetByteCount(body);
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
    /// Packs items into batches that stay within both a maximum count and a maximum combined size, keeping their order.
    /// </summary>
    /// <param name="items">The items to pack.</param>
    /// <param name="sizeOf">A delegate that returns the size, in bytes, of an item.</param>
    /// <param name="maximumCount">The maximum number of items in a batch.</param>
    /// <param name="maximumSize">The maximum combined size, in bytes, of the items in a batch.</param>
    public static List<List<T>> Pack<T>(IEnumerable<T> items, Func<T, int> sizeOf, int maximumCount, int maximumSize)
    {
        var batches = new List<List<T>>();
        var batch = new List<T>(maximumCount);
        int batchSize = 0;

        foreach (var item in items)
        {
            int size = sizeOf(item);

            if (batch.Count > 0 && (batch.Count >= maximumCount || batchSize + size > maximumSize))
            {
                batches.Add(batch);
                batch = new List<T>(maximumCount);
                batchSize = 0;
            }

            batch.Add(item);
            batchSize += size;
        }

        if (batch.Count > 0)
        {
            batches.Add(batch);
        }

        return batches;
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
