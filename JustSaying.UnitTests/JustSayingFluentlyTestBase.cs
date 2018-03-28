using System.Reflection;
using JustSaying.AwsTools.QueueCreation;
using JustBehave;
using Microsoft.Extensions.Logging;
using NSubstitute;

namespace JustSaying.UnitTests
{
    public abstract class JustSayingFluentlyTestBase : XAsyncBehaviourTest<JustSaying.JustSayingFluently>
    {
        protected IPublishConfiguration Configuration;
        protected IAmJustSaying Bus;
        protected readonly IVerifyAmazonQueues QueueVerifier = Substitute.For<IVerifyAmazonQueues>();

        protected override JustSaying.JustSayingFluently CreateSystemUnderTest()
        {
            if (Configuration == null)
            {
                Configuration = new MessagingConfig();
            }

            var fns = CreateMeABus
                .WithLogging(new LoggerFactory())
                .InRegion("defaultRegion")
                .WithFailoverRegion("failoverRegion")
                .WithActiveRegion(() => "defaultRegion")
                .ConfigurePublisherWith(x =>
                {
                    x.PublishFailureBackoffMilliseconds = Configuration.PublishFailureBackoffMilliseconds;
                    x.PublishFailureReAttempts = Configuration.PublishFailureReAttempts;

                }) as JustSaying.JustSayingFluently;

            ConfigureNotificationStackMock(fns);
            ConfigureAmazonQueueCreator(fns);

            return fns;
        }

        // ToDo: Must do better!!
        private void ConfigureNotificationStackMock(JustSaying.JustSayingFluently fns)
        {
            Bus = Substitute.For<IAmJustSaying>();

            var notificationStackField = fns.GetType().GetField("Bus", BindingFlags.Instance | BindingFlags.NonPublic);

            var constructedStack = (JustSaying.JustSayingBus)notificationStackField.GetValue(fns);

            Bus.Config.Returns(constructedStack.Config);

            notificationStackField.SetValue(fns, Bus);
        }

        private void ConfigureAmazonQueueCreator(JustSaying.JustSayingFluently fns)
        {
            fns.GetType()
                .GetField("_amazonQueueCreator", BindingFlags.Instance | BindingFlags.NonPublic)?.SetValue(fns, QueueVerifier);
        }
    }
}
