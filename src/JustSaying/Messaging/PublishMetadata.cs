using System.Globalization;
using Amazon.SimpleNotificationService.Model;

namespace JustSaying.Messaging;

public class PublishMetadata
{
    public TimeSpan? Delay { get; set; }

    public IDictionary<string, MessageAttributeValue> MessageAttributes { get; private set; }

    public PublishMetadata AddMessageAttribute(string key, IReadOnlyCollection<byte> data)
    {
        if (data == null)
        {
            throw new ArgumentNullException(nameof(data));
        }

        var mav = new MessageAttributeValue();
        mav.BinaryValue = new MemoryStream(data.ToArray());
        mav.DataType = "Binary";

        MessageAttributes[key] = mav;

        return this;
    }

    public PublishMetadata AddMessageAttribute(string key, MessageAttributeValue value)
    {
        if (MessageAttributes == null)
        {
            MessageAttributes = new Dictionary<string, MessageAttributeValue>(StringComparer.Ordinal);
        }

        MessageAttributes[key] = value;

        return this;
    }

    public PublishMetadata AddMessageAttribute(string key, string value)
    {
        return AddMessageAttribute(key, new MessageAttributeValue
        {
            StringValue = value,
            DataType = "String"
        });
    }

    public PublishMetadata AddMessageAttribute(string key, double value)
    {
        return AddMessageAttribute(key, new MessageAttributeValue
        {
            StringValue = value.ToString(CultureInfo.InvariantCulture),
            DataType = "Number"
        });
    }

}
