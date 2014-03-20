using System;
using System.Reflection;
using JustEat.Simples.NotificationStack.AwsTools.QueueCreation;
using JustEat.Testing;
using NSubstitute;

namespace SimpleMessageMule.UnitTests
{
    public abstract class FluentMessageMuleTestBase : BehaviourTest<FluentMessagingMule>
    {
        protected INotificationStackConfiguration Configuration;
        protected INotificationStack NotificationStack;
        protected override void Given()
        {
            throw new NotImplementedException();
        }

        protected override FluentMessagingMule CreateSystemUnderTest()
        {
            if (Configuration == null)
            {
                Configuration = new MessagingConfig { Region = "defaultRegion" };
            }

            var fns = FluentMessagingMule.Register(x =>
            {
                x.PublishFailureBackoffMilliseconds = Configuration.PublishFailureBackoffMilliseconds;
                x.PublishFailureReAttempts = Configuration.PublishFailureReAttempts;
                x.Region = Configuration.Region;
            }).WithMonitoring(null) as FluentMessagingMule;


            ConfigureNotificationStackMock(fns);

            ConfigureAmazonQueueCreator(fns);

            return fns;
        }

        // ToDo: Must do btter!!
        private void ConfigureNotificationStackMock(FluentMessagingMule fns)
        {
            NotificationStack = Substitute.For<INotificationStack>();

            var notificationStackField = fns.GetType().GetField("Stack", BindingFlags.Instance | BindingFlags.NonPublic);

            var constructedStack = (SimpleMessageMule.NotificationStack)notificationStackField.GetValue(fns);

            NotificationStack.Config.Returns(constructedStack.Config);

            notificationStackField.SetValue(fns, NotificationStack);
        }

        private void ConfigureAmazonQueueCreator(FluentMessagingMule fns)
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