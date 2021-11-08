using JustSaying.Messaging.MessageSerialization;
using JustSaying.Models;
using Shouldly;
using Xunit;

namespace JustSaying.UnitTests.Messaging.Serialization.SerializationRegister;

public class WhenAddingASerializerTwice : XBehaviourTest<MessageSerializationRegister>
{
    protected override MessageSerializationRegister CreateSystemUnderTest() =>
        new MessageSerializationRegister(
            new NonGenericMessageSubjectProvider(),
            new NewtonsoftSerializationFactory());

    protected override void Given()
    {
        RecordAnyExceptionsThrown();
    }

    protected override void WhenAction()
    {
        SystemUnderTest.AddSerializer<Message>();
        SystemUnderTest.AddSerializer<Message>();
    }

    [Fact]
    public void ExceptionIsNotThrown()
    {
        ThrownException.ShouldBeNull();
    }
}