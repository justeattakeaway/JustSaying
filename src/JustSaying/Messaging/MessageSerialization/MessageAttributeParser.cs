namespace JustSaying.Messaging.MessageSerialization;

internal static class MessageAttributeParser
{
    public static MessageAttributeValue Parse(string dataType, string dataValue)
    {
        // Check for a prefix instead of an exact match as SQS supports custom-type labels, or example, "Binary.gif".
        // See https://docs.aws.amazon.com/AWSSimpleQueueService/latest/SQSDeveloperGuide/sqs-message-metadata.html#sqs-message-attributes.
        bool isBinary = dataType?.StartsWith("Binary", StringComparison.Ordinal) is true;

        return new()
        {
            DataType = dataType,
            StringValue = !isBinary ? dataValue : null,
            BinaryValue = isBinary ? Convert.FromBase64String(dataValue) : null
        };
    }
}
