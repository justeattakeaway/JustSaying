using JustSaying.Messaging;
using JustSaying.Stack;
using JustSaying.Stack.Lookups;
using JustEat.Testing;
using NSubstitute;
using NUnit.Framework;

namespace Stack.UnitTests.Lookups.SnsEndpointNames
{
    public class WhenRequestingAnEndpointName : BehaviourTest<SnsPublishEndpointProvider>
    {
        private readonly IMessagingConfig _config = Substitute.For<IMessagingConfig>();

        private string _result;

        protected override SnsPublishEndpointProvider CreateSystemUnderTest()
        {
            return new SnsPublishEndpointProvider(_config, null);
        }

        protected override void Given()
        {
            _config.Environment.Returns("QAxx");
            _config.Tenant.Returns("OuterHebredies");
        }

        protected override void When()
        {
            _result = SystemUnderTest.GetLocationName("OrderDispatch");
        }

        [Then]
        public void LocationIsBuiltInCorrectStructure()
        {
            Assert.AreEqual("outerhebredies-qaxx-orderdispatch", _result);
        }
    }
}
