using System;
using System.Configuration;
using JustEat.Simples.Common.Services;
using JustEat.Testing;
using NUnit.Framework;
using System.Text.RegularExpressions;

namespace JustEat.Simples.Common.UnitTests.Services.IdGeneration
{
    public class WhenGeneratingNewShortGuid : BehaviourTest<ShortGuidGenerator>
    {
        private string _actualValue;

        protected override void Given()
        {
        }

        protected override void When()
        {
            _actualValue = SystemUnderTest.NewId();
        }

        [Then]
        public void ShortGuidAsExpected()
        {
            Assert.IsNotNullOrEmpty(_actualValue);
            Assert.AreEqual(_actualValue.Length, 22);
            Assert.IsTrue(Regex.IsMatch(_actualValue, @"^[a-zA-Z0-9]+$"));
        }
    }
}