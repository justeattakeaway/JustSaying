using JustBehave;
using JustSaying.Messaging.MessageSerialisation;
using JustSaying.TestingFramework;
using NUnit.Framework;

namespace JustSaying.Messaging.UnitTests.Serialisation.Newtonsoft
{
    public class WhenAskingForANewSerialiser : BehaviourTest<NewtonsoftSerialisationFactory>
    {
        private IMessageSerialiser _result;

        protected override void Given()
        {
            
        }

        protected override void When()
        {
            _result = SystemUnderTest.GetSerialiser<GenericMessage>();
        }

        [Then]
        public void OneIsProvided()
        {
            Assert.NotNull(_result);
        }
    }
}