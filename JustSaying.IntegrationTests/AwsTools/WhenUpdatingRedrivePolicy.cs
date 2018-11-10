using System.Threading.Tasks;
using JustSaying.AwsTools.QueueCreation;
using Shouldly;
using Xunit;

namespace JustSaying.IntegrationTests.AwsTools
{
    [Collection(GlobalSetup.CollectionName)]
    public class WhenUpdatingRedrivePolicy : WhenCreatingQueuesByName
    {
        private int _newMaximumReceived;

        protected override Task Given()
        {
            _newMaximumReceived = 2;

            return base.Given();
        }

        protected override async Task When()
        {
            await SystemUnderTest.CreateAsync(new SqsBasicConfiguration());

            await SystemUnderTest.UpdateRedrivePolicyAsync(
                new RedrivePolicy(_newMaximumReceived, SystemUnderTest.ErrorQueue.Arn));
        }

        [AwsFact]
        public void TheRedrivePolicyIsUpdatedWithTheNewValue()
        {
            SystemUnderTest.RedrivePolicy.MaximumReceives.ShouldBe(_newMaximumReceived);
        }
    }
}
