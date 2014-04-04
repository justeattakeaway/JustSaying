using System;
using JustSaying.AwsTools.QueueCreation;
using JustSaying.Messaging;
using JustSaying.Stack;
using JustSaying.Stack.Lookups;
using JustEat.Testing;
using NSubstitute;
using NUnit.Framework;

namespace UnitTests.Lookups.SqsEndpointNames
{
    public class WhenRequestingAnEndpointNameForASpecificInstanceNumberOfZero : BehaviourTest<SqsSubscribtionEndpointProvider>
    {
        private readonly IMessagingConfig _publishConfig = Substitute.For<IMessagingConfig>();
        private readonly SqsConfiguration _sqsConfiguration = new SqsConfiguration();

        protected override SqsSubscribtionEndpointProvider CreateSystemUnderTest()
        {
            return new SqsSubscribtionEndpointProvider(_sqsConfiguration, _publishConfig);
        }

        protected override void Given()
        {
            _publishConfig.Environment.Returns("QAxx");
            _publishConfig.Tenant.Returns("OuterHebredies");

            _sqsConfiguration.Topic = "OrderDispatch";
            _sqsConfiguration.InstancePosition = 0;

            RecordAnyExceptionsThrown();
        }

        protected override void When()
        {
            SystemUnderTest.GetLocationName();
        }

        [Then]
        public void SillyInstancePositionsAreNotAllowed()
        {
            Assert.IsInstanceOf<ArgumentOutOfRangeException>(ThrownException);
        }
    }
}