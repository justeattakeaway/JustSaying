using System.Xml.Serialization;

namespace JustSaying.UnitTests.FakeMessages.Xml
{
    [XmlRoot(ElementName = "Attribute")]
    public class SqsAttribute
    {
        [XmlElement(ElementName = "Name")]
        public string Name { get; set; }

        [XmlElement(ElementName = "Value")]
        public string Value { get; set; }
    }
}
