using System;
using JustEat.Simples.NotificationStack.Messaging;
using JustEat.Simples.NotificationStack.Messaging.Lookups;
using JustEat.Testing;
using NSubstitute;
using NUnit.Framework;

namespace UnitTests.Lookups.SqsEndpointNames
{
    public class WhenRequestingAnEndpointNameForASpecificInstanceNumberOfZero : BehaviourTest<SqsSubscribtionEndpointProvider>
    {
        private readonly IMessagingConfig _config = Substitute.For<IMessagingConfig>();

        protected override SqsSubscribtionEndpointProvider CreateSystemUnderTest()
        {
            return new SqsSubscribtionEndpointProvider(_config);
        }

        protected override void Given()
        {
            _config.Environment.Returns("QAxx");
            _config.Tenant.Returns("OuterHebredies");
            RecordAnyExceptionsThrown();
        }

        protected override void When()
        {
            SystemUnderTest.GetLocationName("BoxHandler", "OrderDispatch", 0);
        }

        [Then]
        public void SillyInstancePositionsAreNotAllowed()
        {
            Assert.IsInstanceOf<ArgumentOutOfRangeException>(ThrownException);
        }
    }
}