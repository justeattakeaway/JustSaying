using JustSaying.AwsTools.QueueCreation;
using NUnit.Framework;

namespace JustSaying.AwsTools.IntegrationTests
{
    public class WhenUpdatingRedrivePolicy : WhenCreatingQueuesByName
    {
        private int _newMaximumReceived;

        protected override void Given()
        {
            _newMaximumReceived = 2;

            base.Given();
        }

        protected override void When()
        {

            SystemUnderTest.Create(new SqsBasicConfiguration());

            SystemUnderTest.UpdateRedrivePolicy(new RedrivePolicy(_newMaximumReceived, SystemUnderTest.ErrorQueue.Arn));
        }

        [Test]
        public void TheRedrivePolicyIsUpdatedWithTheNewValue()
        {
            Assert.AreEqual(_newMaximumReceived, SystemUnderTest.RedrivePolicy.MaximumReceives);
        }
    }
}