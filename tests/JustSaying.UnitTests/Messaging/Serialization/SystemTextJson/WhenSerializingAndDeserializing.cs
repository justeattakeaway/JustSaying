using JustSaying.Messaging.MessageSerialization;
using JustSaying.TestingFramework;

namespace JustSaying.UnitTests.Messaging.Serialization.SystemTextJson;

#pragma warning disable CS0618 // Type or member is obsolete
public class WhenSerializingAndDeserializing : XBehaviourTest<SystemTextJsonSerializer>
#pragma warning restore CS0618 // Type or member is obsolete
{
    private MessageWithEnum _messageOut;
    private MessageWithEnum _messageIn;
    private string _jsonMessage;

    protected override void Given()
    {
        _messageOut = new MessageWithEnum() { EnumVal = Value.Two };
    }

    protected override void WhenAction()
    {
        _jsonMessage = SystemUnderTest.Serialize(_messageOut, false, _messageOut.GetType().Name);
        _messageIn = SystemUnderTest.Deserialize(_jsonMessage, typeof(MessageWithEnum)) as MessageWithEnum;
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
