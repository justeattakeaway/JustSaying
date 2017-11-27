using JustBehave;
using JustSaying.Messaging.MessageSerialisation;
using JustSaying.TestingFramework;
using Shouldly;
using Xunit;

namespace JustSaying.UnitTests.Messaging.Serialisation.Newtonsoft
{
    public class WhenAskingForANewSerialiser : XBehaviourTest<NewtonsoftSerialisationFactory>
    {
        private IMessageSerialiser _result;

        protected override void Given()
        {
            
        }

        protected override void When()
        {
            _result = SystemUnderTest.GetSerialiser<GenericMessage>();
        }

        [Fact]
        public void OneIsProvided()
        {
            _result.ShouldNotBeNull();
        }
    }
}
