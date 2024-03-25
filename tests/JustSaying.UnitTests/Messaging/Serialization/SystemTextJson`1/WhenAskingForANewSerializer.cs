using JustSaying.Messaging.MessageSerialization;
using JustSaying.TestingFramework;

namespace JustSaying.UnitTests.Messaging.Serialization.SystemTextJson_1;

public class WhenAskingForANewSerializer : XBehaviourTest<SystemTextJsonSerializationFactory>
{
    private IMessageSerializer _result;

    protected override void Given()
    {
    }

    protected override void WhenAction()
    {
        _result = SystemUnderTest.GetSerializer<SimpleMessage>();
    }

    [Fact]
    public void OneIsProvided()
    {
        _result.ShouldNotBeNull();
    }
}
