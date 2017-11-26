using JustBehave;
using JustSaying.Messaging.MessageSerialisation;
using JustSaying.TestingFramework;
using Xunit;
using Assert = NUnit.Framework.Assert;

namespace JustSaying.Messaging.UnitTests.Serialisation.Newtonsoft
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
            Assert.NotNull(_result);
        }
    }
}
