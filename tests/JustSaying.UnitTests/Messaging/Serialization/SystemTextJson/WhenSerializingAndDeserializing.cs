using JustSaying.Messaging.MessageSerialization;
using JustSaying.TestingFramework;

namespace JustSaying.UnitTests.Messaging.Serialization.SystemTextJson;

public class WhenSerializingAndDeserializing : XBehaviourTest<SystemTextJsonMessageBodySerializer<MessageWithEnum>>
{
    private MessageWithEnum _messageOut;
    private MessageWithEnum _messageIn;
    private string _jsonMessage;

    protected override SystemTextJsonMessageBodySerializer<MessageWithEnum> CreateSystemUnderTest()
    {
        return new SystemTextJsonMessageBodySerializer<MessageWithEnum>(SystemTextJsonMessageBodySerializer.DefaultJsonSerializerOptions);
    }

    protected override void Given()
    {
        _messageOut = new MessageWithEnum { EnumVal = Value.Two };
    }

    protected override void WhenAction()
    {
        _jsonMessage = SystemUnderTest.Serialize(_messageOut);
        _messageIn = SystemUnderTest.Deserialize(_jsonMessage) as MessageWithEnum;
    }

    [Fact]
    public void MessageHasBeenCreated()
    {
        _messageOut.ShouldNotBeNull();
    }

    [Fact]
    public void MessagesContainSameDetails()
    {
        _messageOut.EnumVal.ShouldBe(_messageIn.EnumVal);
        _messageOut.RaisingComponent.ShouldBe(_messageIn.RaisingComponent);
        _messageOut.TimeStamp.ShouldBe(_messageIn.TimeStamp);
    }

    [Fact]
    public void EnumsAreRepresentedAsStrings()
    {
        _jsonMessage.ShouldContain("EnumVal");
        _jsonMessage.ShouldContain("Two");
    }
}
