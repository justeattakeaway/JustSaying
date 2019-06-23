using System.Xml.Serialization;

namespace JustSaying.UnitTests.FakeMessages.Xml
{
    [XmlRoot(ElementName = "GetQueueUrlResult")]
    public class SqsGetQueueUrlResult
    {
        [XmlElement(ElementName = "QueueUrl")]
        public string QueueUrl { get; set; }
    }
}
