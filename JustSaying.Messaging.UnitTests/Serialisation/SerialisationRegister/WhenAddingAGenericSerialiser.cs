using JustBehave;
using JustSaying.Messaging.MessageSerialisation;
using JustSaying.Models;
using NUnit.Framework;

namespace JustSaying.Messaging.UnitTests.Serialisation.SerialisationRegister
{
    public class WhenAddingAGenericSerialiser : BehaviourTest<MessageSerialisationRegister>
    {
        private readonly NewtonsoftSerialiser _serialiser = new NewtonsoftSerialiser();

        protected override void Given() { }

        protected override void When()
        {
            SystemUnderTest.AddSerialiser<Message>(_serialiser);
        }

        [Then]
        public void MappingsCanBeRetreivedByStringType()
        {
            Assert.NotNull(SystemUnderTest.GeTypeSerialiser(typeof(Message).Name));
        }

        [Test]
        public void MappingsCanBeRetreivedStronglyTyped()
        {
            Assert.NotNull(SystemUnderTest.GeTypeSerialiser(typeof(Message)));
        }
    }
}