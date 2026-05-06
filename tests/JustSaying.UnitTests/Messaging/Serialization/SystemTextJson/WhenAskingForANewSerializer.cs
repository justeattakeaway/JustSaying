using System.Text.Json;
using JustSaying.Messaging.MessageSerialization;
using JustSaying.TestingFramework;

namespace JustSaying.UnitTests.Messaging.Serialization.SystemTextJson;

public class WhenAskingForANewSerializer : XBehaviourTest<SystemTextJsonSerializationFactory>
{
    private IMessageBodySerializer _result;

    protected override SystemTextJsonSerializationFactory CreateSystemUnderTest()
    {
        return new SystemTextJsonSerializationFactory(new JsonSerializerOptions());
    }

    protected override void Given()
    {
    }

    protected override void WhenAction()
    {
        _result = SystemUnderTest.GetSerializer<SimpleMessage>();
    }

    [Test]
    public void OneIsProvided()
    {
        _result.ShouldNotBeNull();
    }
}
