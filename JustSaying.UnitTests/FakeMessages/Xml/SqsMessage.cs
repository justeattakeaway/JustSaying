using System.Collections.Generic;
using System.Xml.Serialization;

namespace JustSaying.UnitTests.FakeMessages.Xml
{
    [XmlRoot(ElementName = "Message")]
    public class SqsMessage
    {
        [XmlElement(ElementName = "MessageId")]
        public string MessageId { get; set; }

        [XmlElement(ElementName = "ReceiptHandle")]
        public string ReceiptHandle { get; set; }

        [XmlElement(ElementName = "MD5OfBody")]
        public string MD5OfBody { get; set; }

        [XmlElement(ElementName = "Body")]
        public string Body { get; set; }

        [XmlElement(ElementName = "Attribute")]
        public List<SqsAttribute> Attributes { get; set; }
    }
}
