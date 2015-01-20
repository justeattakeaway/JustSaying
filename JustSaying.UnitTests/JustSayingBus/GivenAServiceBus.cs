using System;
using JustBehave;
using JustSaying.Messaging.MessageSerialisation;
using JustSaying.Messaging.Monitoring;
using NSubstitute;

namespace JustSaying.UnitTests.JustSayingBus
{
    public abstract class GivenAServiceBus : BehaviourTest<JustSaying.JustSayingBus>
    {
        protected IMessagingConfig Config;
        protected IMessageMonitor Monitor;
        protected IMessageSerialisationRegister MessageSerialisationRegister;

        protected override void Given()
        {
            Config = Substitute.For<IMessagingConfig>();
            Monitor = Substitute.For<IMessageMonitor>();
            MessageSerialisationRegister = Substitute.For<IMessageSerialisationRegister>();
            MessageSerialisationRegister.GeTypeSerialiser(Arg.Any<Type>()).Returns(new TypeSerialiser(typeof(Type), Substitute.For<IMessageSerialiser>()));
        
        }

        protected override JustSaying.JustSayingBus CreateSystemUnderTest()
        {
            return new JustSaying.JustSayingBus(Config, MessageSerialisationRegister) { Monitor = Monitor };
        }
    }
}