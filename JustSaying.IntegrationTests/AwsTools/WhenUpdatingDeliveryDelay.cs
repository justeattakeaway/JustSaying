using System.Threading.Tasks;
using JustSaying.AwsTools.QueueCreation;
using Shouldly;
using Xunit;

namespace JustSaying.IntegrationTests.AwsTools
{
    [Collection(GlobalSetup.CollectionName)]
    public class WhenUpdatingDeliveryDelay : WhenCreatingQueuesByName
    {
        private int _oldDeliveryDelay;
        private int _newDeliveryDelay;

        protected override void Given()
        {
            _oldDeliveryDelay = 120;
            _newDeliveryDelay = 300;

            base.Given();
        }

        protected override async Task When()
        {
            var queueConfig = new SqsBasicConfiguration
            {
                DeliveryDelaySeconds = _oldDeliveryDelay
            };

            await SystemUnderTest.CreateAsync(queueConfig);

            queueConfig.DeliveryDelaySeconds = _newDeliveryDelay;

            await SystemUnderTest.UpdateQueueAttributeAsync(queueConfig);
        }

        [AwsFact]
        public void TheDeliveryDelayIsUpdatedWithTheNewValue()
        {
            SystemUnderTest.DeliveryDelay.ShouldBe(_newDeliveryDelay);
        }
    }
}
