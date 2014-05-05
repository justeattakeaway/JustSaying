using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using JustEat.Testing;
using JustSaying.AwsTools.QueueCreation;
using NUnit.Framework;

namespace JustSaying.AwsTools.UnitTests.QueueCreation
{
    [TestFixture]
    class WhenSerializingRedrivePolicy 
    {
        [Test]
        public void CanDeserializeIntoRedrivePolicy()
        {
            var policy = new RedrivePolicy(1, "queue");
            var policySerialized = policy.ToString();

            var outputPolicy = RedrivePolicy.ConvertFromString(policySerialized);

            Assert.AreEqual(policy.MaximumReceives, outputPolicy.MaximumReceives);
            Assert.AreEqual(policy.DeadLetterQueue, outputPolicy.DeadLetterQueue);
        }
    }
}
