using JustSaying.Messaging.MessageSerialization;
using JustSaying.TestingFramework;

namespace JustSaying.UnitTests.Messaging.Serialization.Newtonsoft;

public class WhenUsingDefaultSerializationFactory : XBehaviourTest<NewtonsoftMessageBodySerializer<MessageWithEnum>>
{
    private MessageWithEnum _messageOut;
    private string _jsonMessage;

    protected override NewtonsoftMessageBodySerializer<MessageWithEnum> CreateSystemUnderTest()
    {
        // This is the path used when no custom settings are provided,
        // which is the default for all DI registrations.
        var factory = new NewtonsoftSerializationFactory();
        return (NewtonsoftMessageBodySerializer<MessageWithEnum>)factory.GetSerializer<MessageWithEnum>();
    }

    protected override void Given()
    {
        _messageOut = new MessageWithEnum { EnumVal = Value.Two };
    }

    protected override void WhenAction()
    {
        _jsonMessage = SystemUnderTest.Serialize(_messageOut);
    }

    [Test]
    public void MessageHasBeenCreated()
    {
        _messageOut.ShouldNotBeNull();
    }

    [Test]
    public void EnumsAreRepresentedAsStrings()
    {
        _jsonMessage.ShouldContain("\"EnumVal\":\"Two\"");
    }
}
