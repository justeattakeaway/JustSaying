using System.Xml.Serialization;

namespace JustSaying.UnitTests.FakeMessages.Xml
{
    [XmlRoot(ElementName = "GetQueueUrlResponse")]
    public class SqsGetQueueUrlResponse
    {
        [XmlElement(ElementName = "GetQueueUrlResult")]
        public SqsGetQueueUrlResult GetQueueUrlResult { get; set; }

        [XmlElement(ElementName = "ResponseMetadata")]
        public SqsResponseMetadata ResponseMetadata { get; set; }
    }
}
