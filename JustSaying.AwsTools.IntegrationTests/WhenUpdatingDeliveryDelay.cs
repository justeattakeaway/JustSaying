using JustSaying.AwsTools.QueueCreation;
using NUnit.Framework;

namespace JustSaying.AwsTools.IntegrationTests
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

        protected override void When()
        {
            SystemUnderTest.Create(new SqsBasicConfiguration { DeliveryDelaySeconds = _oldDeliveryDelay });

            SystemUnderTest.UpdateQueueAttributeAsync(
                new SqsBasicConfiguration { DeliveryDelaySeconds = _newDeliveryDelay })
                .GetAwaiter().GetResult();
        }

        [Test]
        public void TheDeliveryDelayIsUpdatedWithTheNewValue()
        {
            Assert.AreEqual(_newDeliveryDelay, SystemUnderTest.DeliveryDelay);
        }
    }
}
