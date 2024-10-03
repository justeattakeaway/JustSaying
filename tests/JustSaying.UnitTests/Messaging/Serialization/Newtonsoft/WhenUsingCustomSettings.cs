using JustSaying.Messaging.MessageSerialization;
using JustSaying.TestingFramework;
using Newtonsoft.Json;

namespace JustSaying.UnitTests.Messaging.Serialization.Newtonsoft;

public class WhenUsingCustomSettings : XBehaviourTest<NewtonsoftMessageBodySerializer<MessageWithEnum>>
{
    private MessageWithEnum _messageOut;
    private string _jsonMessage;

    protected override NewtonsoftMessageBodySerializer<MessageWithEnum> CreateSystemUnderTest()
    {
        return new NewtonsoftMessageBodySerializer<MessageWithEnum>(new JsonSerializerSettings());
    }

    protected override void Given()
    {
        _messageOut = new MessageWithEnum() { EnumVal = Value.Two };
    }

    public string GetMessageInContext(MessageWithEnum message)
    {
        var context = new { Subject = message.GetType().Name, Message = SystemUnderTest.Serialize(message) };
        return JsonConvert.SerializeObject(context);
    }

    protected override void WhenAction()
    {
        _jsonMessage = GetMessageInContext(_messageOut);
    }

    [Fact]
    public void MessageHasBeenCreated()
    {
        _messageOut.ShouldNotBeNull();
    }

    [Fact]
    public void EnumsAreNotRepresentedAsStrings()
    {
        _jsonMessage.ShouldContain("EnumVal");
        _jsonMessage.ShouldNotContain("Two");
    }
}
