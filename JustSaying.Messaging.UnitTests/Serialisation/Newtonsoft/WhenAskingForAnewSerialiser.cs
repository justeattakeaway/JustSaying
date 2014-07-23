using JustBehave;
using JustSaying.Messaging.MessageSerialisation;
using JustSaying.Models;
using JustSaying.TestingFramework;
using NUnit.Framework;

namespace JustSaying.Messaging.UnitTests.Serialisation.Newtonsoft
{
    public class WhenAskingForAnewSerialiser : BehaviourTest<NewtonsoftSerialisationFactory>
    {
        private IMessageSerialiser<Message> _result;

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