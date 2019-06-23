using System.Xml.Serialization;

namespace JustSaying.UnitTests.FakeMessages.Xml
{
    [XmlRoot(ElementName = "GetQueueAttributesResponse")]
    public class SqsGetQueueAttributesResponse
    {
        [XmlElement(ElementName = "GetQueueAttributesResult")]
        public SqsGetQueueAttributesResult GetQueueAttributesResult { get; set; }

        [XmlElement(ElementName = "ResponseMetadata")]
        public SqsResponseMetadata ResponseMetadata { get; set; }
    }
}
