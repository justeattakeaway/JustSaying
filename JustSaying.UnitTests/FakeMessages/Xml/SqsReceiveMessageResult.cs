using System.Collections.Generic;
using System.Xml.Serialization;

namespace JustSaying.UnitTests.FakeMessages.Xml
{
    [XmlRoot(ElementName = "ReceiveMessageResult")]
    public class SqsReceiveMessageResult
    {
        [XmlElement(ElementName = "Message")]
        public List<SqsMessage> Messages { get; set; }
    }
}
