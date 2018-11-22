using System.Reflection;
using System.Threading.Tasks;
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

        protected override Task<JustSaying.JustSayingFluently> CreateSystemUnderTestAsync()
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
                    x.PublishFailureBackoff = Configuration.PublishFailureBackoff;
                    x.PublishFailureReAttempts = Configuration.PublishFailureReAttempts;

                }) as JustSaying.JustSayingFluently;

            ConfigureNotificationStackMock(fns);
            ConfigureAmazonQueueCreator(fns);

            return Task.FromResult(fns);
        }

        // ToDo: Must do better!!
        private void ConfigureNotificationStackMock(JustSaying.JustSayingFluently fns)
        {
            var constructedStack = (JustSaying.JustSayingBus)fns.Bus;

            Bus = Substitute.For<IAmJustSaying>();
            Bus.Config.Returns(constructedStack.Config);

            fns.Bus = Bus;
        }

        private void ConfigureAmazonQueueCreator(JustSaying.JustSayingFluently fns)
        {
            fns.GetType()
                .GetField("_amazonQueueCreator", BindingFlags.Instance | BindingFlags.NonPublic)?.SetValue(fns, QueueVerifier);
        }
    }
}
