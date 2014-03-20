using JustEat.Testing;
using NUnit.Framework;
using SimpleMessageMule.Lookups;

namespace SimpleMessageMule.UnitTests.Lookups.SnsEndpointNames
{
    public class WhenRequestingAnEndpointName : BehaviourTest<SnsPublishEndpointProvider>
    {
        private string _result;

        protected override SnsPublishEndpointProvider CreateSystemUnderTest()
        {
            return new SnsPublishEndpointProvider("OrderDispatch");
        }

        protected override void Given()
        {
        }

        protected override void When()
        {
            _result = SystemUnderTest.GetLocationName();
        }

        [Then]
        public void LocationIsBuiltInCorrectStructure()
        {
            Assert.AreEqual("orderdispatch", _result);
        }
    }
}
