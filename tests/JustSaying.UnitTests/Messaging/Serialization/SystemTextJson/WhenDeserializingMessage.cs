using JustSaying.Messaging.MessageSerialization;
using JustSaying.Models;

namespace JustSaying.UnitTests.Messaging.Serialization.SystemTextJson;

public class WhenDeserializingMessage : XBehaviourTest<MessageSerializationRegister>
{
    protected override MessageSerializationRegister CreateSystemUnderTest() =>
        new(
            new NonGenericMessageSubjectProvider(),
            new SystemTextJsonSerializationFactory());

    protected override void Given()
    {
        RecordAnyExceptionsThrown();
    }

    protected override void WhenAction()
    {
        SystemUnderTest.AddSerializer<CustomMessage>();
    }

    [Fact]
    public void DeserializesMessage()
    {
        // Arrange
        var body =
            """
            {
              "Subject": "CustomMessage",
              "Message":"{\"Custom\":\"Text\"}"
            }
            """;

        // Act
        var actual = SystemUnderTest.DeserializeMessage(body);

        // Assert
        actual.ShouldNotBeNull();
        actual.Message.ShouldNotBeNull();
        actual.MessageAttributes.ShouldNotBeNull();

        var message = actual.Message.ShouldBeOfType<CustomMessage>();

        message.Custom.ShouldBe("Text");
    }

    [Fact]
    public void DeserializesMessageWithMessageAttributes()
    {
        // Arrange
        var body =
            """
            {
              "Subject": "CustomMessage",
              "Message":"{\"Custom\":\"Text\"}",
              "MessageAttributes": {
                "Text": {
                  "Type": "String",
                  "Value": "foo"
                },
                "Integer": {
                  "Type": "Number",
                  "Value": "42"
                },
                "BinaryData": {
                  "Type": "Binary",
                  "Value": "SnVzdCBFYXQgVGFrZWF3YXkuY29t"
                },
                "CustomBinaryData": {
                  "Type": "Binary.jet",
                  "Value": "SnVzdFNheWluZw=="
                }
              }
            }
            """;

        // Act
        var actual = SystemUnderTest.DeserializeMessage(body);

        // Assert
        actual.ShouldNotBeNull();
        actual.Message.ShouldNotBeNull();

        var message = actual.Message.ShouldBeOfType<CustomMessage>();
        message.Custom.ShouldBe("Text");

        actual.MessageAttributes.ShouldNotBeNull();

        var attribute = actual.MessageAttributes.Get("Text");

        attribute.ShouldNotBeNull();
        attribute.DataType.ShouldBe("String");
        attribute.StringValue.ShouldBe("foo");
        attribute.BinaryValue.ShouldBeNull();

        attribute = actual.MessageAttributes.Get("Integer");

        attribute.ShouldNotBeNull();
        attribute.DataType.ShouldBe("Number");
        attribute.StringValue.ShouldBe("42");
        attribute.BinaryValue.ShouldBeNull();

        attribute = actual.MessageAttributes.Get("BinaryData");

        attribute.ShouldNotBeNull();
        attribute.DataType.ShouldBe("Binary");
        attribute.StringValue.ShouldBeNull();
        attribute.BinaryValue.ShouldBe([.. "Just Eat Takeaway.com"u8]);

        attribute = actual.MessageAttributes.Get("CustomBinaryData");

        attribute.ShouldNotBeNull();
        attribute.DataType.ShouldBe("Binary.jet");
        attribute.StringValue.ShouldBeNull();
        attribute.BinaryValue.ShouldBe([.. "JustSaying"u8]);
    }

    private sealed class CustomMessage : Message
    {
        public string Custom { get; set; }
    }
}
