using System.Xml.Serialization;

namespace JustSaying.UnitTests.FakeMessages.Xml
{
    [XmlRoot(ElementName = "SetQueueAttributesResponse")]
    public class SqsSetQueueAttributesResponse
    {
        [XmlElement(ElementName = "ResponseMetadata")]
        public SqsResponseMetadata ResponseMetadata { get; set; }
    }
}
