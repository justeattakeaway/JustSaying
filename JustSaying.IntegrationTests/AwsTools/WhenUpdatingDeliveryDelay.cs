using System;
using System.Threading.Tasks;
using JustSaying.AwsTools.QueueCreation;
using Shouldly;
using Xunit;

namespace JustSaying.IntegrationTests.AwsTools
{
    [Collection(GlobalSetup.CollectionName)]
    public class WhenUpdatingDeliveryDelay : WhenCreatingQueuesByName
    {
        private TimeSpan _oldDeliveryDelay;
        private TimeSpan _newDeliveryDelay;

        protected override void Given()
        {
            _oldDeliveryDelay = TimeSpan.FromMinutes(2);
            _newDeliveryDelay = TimeSpan.FromMinutes(5);

            base.Given();
        }

        protected override async Task When()
        {
            var queueConfig = new SqsBasicConfiguration
            {
                DeliveryDelay = _oldDeliveryDelay
            };

            await SystemUnderTest.CreateAsync(queueConfig);

            queueConfig.DeliveryDelay = _newDeliveryDelay;

            await SystemUnderTest.UpdateQueueAttributeAsync(queueConfig);
        }

        [AwsFact]
        public void TheDeliveryDelayIsUpdatedWithTheNewValue()
        {
            SystemUnderTest.DeliveryDelay.ShouldBe(_newDeliveryDelay);
        }
    }
}
