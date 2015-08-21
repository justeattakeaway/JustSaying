using System;
using NUnit.Framework;

namespace JustSaying.UnitTests.Extensions
{
    public class WhenGettingATopicNameWithAPoco : GivenIHaveAMessageType
    {
        protected override Type CreateSystemUnderTest()
        {
            return typeof(Poco);
        }

        [Test]
        public void TheTopicNameComesTypesFromAllTypes()
        {
            Assert.That(Result, Is.EqualTo("poco"));
        } 
    }
}