using System.Threading.Tasks;
using JustSaying.AwsTools.QueueCreation;
using Shouldly;
using Xunit;

namespace JustSaying.IntegrationTests.AwsTools
{
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

        protected override Task When()
        {
            SystemUnderTest.Create(new SqsBasicConfiguration { DeliveryDelaySeconds = _oldDeliveryDelay });

            SystemUnderTest.UpdateQueueAttribute(
                new SqsBasicConfiguration {DeliveryDelaySeconds = _newDeliveryDelay});

            return Task.CompletedTask;
        }

        [Fact]
        public void TheDeliveryDelayIsUpdatedWithTheNewValue()
        {
            SystemUnderTest.DeliveryDelay.ShouldBe(_newDeliveryDelay);
        }
    }
}
