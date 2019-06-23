using System.Xml.Serialization;

namespace JustSaying.UnitTests.FakeMessages.Xml
{
    [XmlRoot(ElementName = "ReceiveMessageResponse")]
    public class SqsReceiveMessageResponse
    {
        [XmlElement(ElementName = "ReceiveMessageResult")]
        public SqsReceiveMessageResult ReceiveMessageResult { get; set; }

        [XmlElement(ElementName = "ResponseMetadata")]
        public SqsResponseMetadata ResponseMetadata { get; set; }
    }
}
