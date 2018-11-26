using JustBehave;
using JustSaying.Messaging.MessageSerialization;
using JustSaying.TestingFramework;
using Shouldly;
using Xunit;

namespace JustSaying.UnitTests.Messaging.Serialization.Newtonsoft
{
    public class WhenAskingForANewSerializer : XBehaviourTest<NewtonsoftSerializationFactory>
    {
        private IMessageSerializer _result;

        protected override void Given()
        {
            
        }

        protected override void When()
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
