using JustEat.Simples.NotificationStack.Messaging;
using JustEat.Simples.NotificationStack.Messaging.Lookups;
using JustEat.Testing;
using NSubstitute;
using NUnit.Framework;

namespace UnitTests.Lookups.SqsEndpointNames
{
    public class WhenRequestingAnEndpointName : BehaviourTest<SqsSubscribtionEndpointProvider>
    {
        private readonly IMessagingConfig _config = Substitute.For<IMessagingConfig>();

        private string _result;

        protected override SqsSubscribtionEndpointProvider CreateSystemUnderTest()
        {
            return new SqsSubscribtionEndpointProvider(_config);
        }

        protected override void Given()
        {
            _config.Environment.Returns("QAxx");
            _config.Tenant.Returns("OuterHebredies");
        }

        protected override void When()
        {
            _result = SystemUnderTest.GetLocationName("BoxHandler", "OrderDispatch");
        }

        [Then]
        public void LocationIsBuiltInCorrectStructure()
        {
            Assert.AreEqual("outerhebredies-qaxx-boxhandler-orderdispatch", _result);
        }
    }

    public class WhenRequestingAnEndpointNameForASpecificInstance : BehaviourTest<SqsSubscribtionEndpointProvider>
    {
        private readonly IMessagingConfig _config = Substitute.For<IMessagingConfig>();

        private string _result;

        protected override SqsSubscribtionEndpointProvider CreateSystemUnderTest()
        {
            return new SqsSubscribtionEndpointProvider(_config);
        }

        protected override void Given()
        {
            _config.Environment.Returns("QAxx");
            _config.Tenant.Returns("OuterHebredies");
        }

        protected override void When()
        {
            _result = SystemUnderTest.GetLocationName("BoxHandler", "OrderDispatch", 99);
        }

        [Then]
        public void LocationIsBuiltInCorrectStructure()
        {
            Assert.AreEqual("outerhebredies-qaxx-boxhandler-99-orderdispatch", _result);
        }
    }
}
