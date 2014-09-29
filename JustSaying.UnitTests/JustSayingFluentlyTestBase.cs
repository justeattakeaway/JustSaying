using System.Reflection;
using JustSaying.AwsTools.QueueCreation;
using JustBehave;
using NSubstitute;

namespace JustSaying.UnitTests
{
    public abstract class JustSayingFluentlyTestBase : BehaviourTest<JustSaying.JustSayingFluently>
    {
        protected IPublishConfiguration Configuration;
        protected IAmJustSaying NotificationStack;
        protected IVerifyAmazonQueues QueueVerifier= Substitute.For<IVerifyAmazonQueues>();

        protected override JustSaying.JustSayingFluently CreateSystemUnderTest()
        {
            if (Configuration == null)
            {
                Configuration = new MessagingConfig { Region = "defaultRegion" };
            }

            var fns = JustSaying.CreateMeABus.InRegion(Configuration.Region).ConfigurePublisherWith(x =>
            {
                x.PublishFailureBackoffMilliseconds = Configuration.PublishFailureBackoffMilliseconds;
                x.PublishFailureReAttempts = Configuration.PublishFailureReAttempts;
                
            }) as JustSaying.JustSayingFluently;
            
            ConfigureNotificationStackMock(fns);

            ConfigureAmazonQueueCreator(fns);

            return fns;
        }

        // ToDo: Must do btter!!
        private void ConfigureNotificationStackMock(JustSaying.JustSayingFluently fns)
        {
            NotificationStack = Substitute.For<IAmJustSaying>();

            var notificationStackField = fns.GetType().GetField("Bus", BindingFlags.Instance | BindingFlags.NonPublic);

            var constructedStack = (JustSaying.JustSayingBus)notificationStackField.GetValue(fns);

            NotificationStack.Config.Returns(constructedStack.Config);

            notificationStackField.SetValue(fns, NotificationStack);
        }

        private void ConfigureAmazonQueueCreator(JustSaying.JustSayingFluently fns)
        {
            fns.GetType()
                .GetField("_amazonQueueCreator", BindingFlags.Instance | BindingFlags.NonPublic)
                .SetValue(fns, QueueVerifier);
        }
    }
}