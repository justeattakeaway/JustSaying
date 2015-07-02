using System;
using NUnit.Framework;

namespace JustSaying.UnitTests.Extensions
{
    public class WhenGettingATopicNameWithANestedGenericType : GivenIHaveAMessageType
    {
        protected override Type CreateSystemUnderTest()
        {
            return typeof(GenericClass<GenericClass<Poco>>);
        }

        [Test]
        public void TheTopicNameComesTypesFromAllTypes()
        {
            Assert.That(Result, Is.EqualTo("genericclass_1-genericclass_1-poco"));
        }
    }
}