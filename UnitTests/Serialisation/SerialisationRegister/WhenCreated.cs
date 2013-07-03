using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using JustEat.Testing;
using NUnit.Framework;
using JustEat.Simples.NotificationStack.Messaging.MessageSerialisation;
using JustEat.Simples.NotificationStack.Messaging.Messages.CustomerCommunication;

namespace UnitTests.Serialisation.SerialisationRegister
{
    [TestFixture]
    public class WhenCreated
    {
        [Test]
        public void ReflectionCreatesMappings()
        {
            var target = new ReflectedMessageSerialisationRegister();
            
            Assert.NotNull(target.GetSerialiser(typeof(CustomerOrderRejectionSms).ToString()));
        }
    }

    //public class WhenCreatedx : BehaviourTest<ReflectedMessageSerialisationRegister>
    //{
    //    protected override void Given()
    //    {
    //        throw new NotImplementedException();
    //    }

    //    protected override void When()
    //    {
            
    //    }

    //    [Then]
    //}
}
