using JustSaying.Messaging.MessageSerialization;
using JustSaying.TestingFramework;
using Shouldly;
using Xunit;

namespace JustSaying.UnitTests.Messaging.Serialization.SystemTextJson
{
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
}
