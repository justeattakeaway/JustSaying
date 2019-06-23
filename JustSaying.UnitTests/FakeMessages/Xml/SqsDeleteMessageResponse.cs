using System.Xml.Serialization;

namespace JustSaying.UnitTests.FakeMessages.Xml
{
    [XmlRoot(ElementName = "DeleteMessageResponse")]
    public class SqsDeleteMessageResponse
    {
        [XmlElement(ElementName = "ResponseMetadata")]
        public SqsResponseMetadata ResponseMetadata { get; set; }
    }
}
