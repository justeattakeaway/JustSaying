using System;
using System.Reflection;
using JustSaying.AwsTools.QueueCreation;
using JustEat.Testing;
using NSubstitute;

namespace JustSaying.UnitTests
{
    public abstract class FluentMessageMuleTestBase : BehaviourTest<JustSayingFluently>
    {
        protected IPublishConfiguration Configuration;
        protected IAmJustSaying NotificationStack;
        protected override void Given()
        {
            throw new NotImplementedException();
        }

        protected override JustSayingFluently CreateSystemUnderTest()
        {
            if (Configuration == null)
            {
                Configuration = new MessagingConfig { Region = "defaultRegion" };
            }

            var fns = Factory.JustSaying(x =>
            {
                x.PublishFailureBackoffMilliseconds = Configuration.PublishFailureBackoffMilliseconds;
                x.PublishFailureReAttempts = Configuration.PublishFailureReAttempts;
                x.Region = Configuration.Region;
            }).WithMonitoring(null) as JustSayingFluently;


            ConfigureNotificationStackMock(fns);

            ConfigureAmazonQueueCreator(fns);

            return fns;
        }

        // ToDo: Must do btter!!
        private void ConfigureNotificationStackMock(JustSayingFluently fns)
        {
            NotificationStack = Substitute.For<IAmJustSaying>();

            var notificationStackField = fns.GetType().GetField("Stack", BindingFlags.Instance | BindingFlags.NonPublic);

            var constructedStack = (JustSaying.JustSayingBus)notificationStackField.GetValue(fns);

            NotificationStack.Config.Returns(constructedStack.Config);

            notificationStackField.SetValue(fns, NotificationStack);
        }

        private void ConfigureAmazonQueueCreator(JustSayingFluently fns)
        {
            fns.GetType()
                .GetField("_amazonQueueCreator", BindingFlags.Instance | BindingFlags.NonPublic)
                .SetValue(fns, Substitute.For<IVerifyAmazonQueues>());
        }

        protected override void When()
        {
            throw new NotImplementedException();
        }
    }
}