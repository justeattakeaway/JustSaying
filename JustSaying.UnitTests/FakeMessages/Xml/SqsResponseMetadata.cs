using System;
using System.Xml.Serialization;

namespace JustSaying.UnitTests.FakeMessages.Xml
{
    [XmlRoot(ElementName = "ResponseMetadata")]
    public class SqsResponseMetadata
    {
        [XmlElement(ElementName = "RequestId")]
        public string RequestId { get; set; } = Guid.NewGuid().ToString();
    }
}
