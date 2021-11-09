using JustSaying.Messaging.MessageSerialization;

namespace JustSaying.UnitTests.Messaging.Serialization.SerializationRegister;

public class WhenDeserializingMessage : XBehaviourTest<MessageSerializationRegister>
{
    protected override MessageSerializationRegister CreateSystemUnderTest() =>
        new MessageSerializationRegister(
            new NonGenericMessageSubjectProvider(),
            new NewtonsoftSerializationFactory());

    private string messageBody = "{'Subject':'nonexistent'}";
    protected override void Given()
    {
        RecordAnyExceptionsThrown();
    }

    protected override void WhenAction()
    {
        SystemUnderTest.AddSerializer<CustomMessage>();
    }

    [Fact]
    public void ThrowsMessageFormatNotSupportedWhenMessabeBodyIsUnserializable()
    {
        Assert.Throws<MessageFormatNotSupportedException>(() => SystemUnderTest.DeserializeMessage(messageBody));
    }
}