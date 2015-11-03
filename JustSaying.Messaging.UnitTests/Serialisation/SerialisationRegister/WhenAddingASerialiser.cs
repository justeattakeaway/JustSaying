using JustBehave;
using JustSaying.Messaging.Extensions;
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
            SystemUnderTest.AddSerialiser<Message>(Substitute.For<IMessageSerialiser>());
        }

        [Then]
        public void MappingsCanBeRetreivedByStringType()
        {
            Assert.NotNull(SystemUnderTest.GeTypeSerialiser(typeof(Message).ToKey()));
        }

        [Test]
        public void MappingsCanBeRetreivedStronglyTyped()
        {
            Assert.NotNull(SystemUnderTest.GeTypeSerialiser(typeof(Message)));
        }
    }
}