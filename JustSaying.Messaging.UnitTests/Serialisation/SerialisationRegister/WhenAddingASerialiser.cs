using JustBehave;
using JustSaying.Messaging.MessageSerialisation;
using JustSaying.Models;
using NSubstitute;
using NUnit.Framework;

namespace JustSaying.Messaging.UnitTests.Serialisation.SerialisationRegister
{
    public class WhenAddingASerialiser : BehaviourTest<MessageSerialisationRegister>
    {
        protected override void Given() { }

        protected override void When()
        {
            SystemUnderTest.AddSerialiser<Message>(Substitute.For<IMessageSerialiser<Message>>());
        }

        [Then]
        public void MappingsCanBeRetreivedByStringType()
        {
            Assert.NotNull(SystemUnderTest.GetSerialiser(typeof(Message).Name));
        }

        [Test]
        public void MappingsCanBeRetreivedStronglyTyped()
        {
            Assert.NotNull(SystemUnderTest.GetSerialiser(typeof(Message)));
        }
    }
}