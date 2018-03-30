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
            await SystemUnderTest.CreateAsync(new SqsBasicConfiguration { DeliveryDelaySeconds = _oldDeliveryDelay });

            await SystemUnderTest.UpdateQueueAttributeAsync(
                new SqsBasicConfiguration {DeliveryDelaySeconds = _newDeliveryDelay});
        }

        [Fact]
        public void TheDeliveryDelayIsUpdatedWithTheNewValue()
        {
            SystemUnderTest.DeliveryDelay.ShouldBe(_newDeliveryDelay);
        }
    }
}
