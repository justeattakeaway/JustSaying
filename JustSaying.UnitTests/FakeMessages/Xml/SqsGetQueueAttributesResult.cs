using System.Collections.Generic;
using System.Xml.Serialization;

namespace JustSaying.UnitTests.FakeMessages.Xml
{
    [XmlRoot(ElementName = "GetQueueAttributesResult")]
    public class SqsGetQueueAttributesResult
    {
        [XmlElement(ElementName = "Attribute")]
        public List<SqsAttribute> Attributes { get; set; }
    }
}
