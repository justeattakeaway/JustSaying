using System;
using System.Threading.Tasks;
using AwsTools.UnitTests.SqsNotificationListener;
using JustSaying.Messaging.MessageHandling;
using JustSaying.TestingFramework;
using NSubstitute;
using NUnit.Framework;

namespace JustSaying.AwsTools.UnitTests.SqsNotificationListener
{
    public class WhenExactlyOnceIsAppliedWithoutSpecificTimeout : BaseQueuePollingTest
    {
        private readonly int _maximumTimeout = (int)TimeSpan.MaxValue.TotalSeconds;

        protected override void Given()
        {
            base.Given();
            MessageLock = Substitute.For<IMessageLock>();
            Handler = new Handler();
        }

        [Test]
        public async Task MessageIsLocked()
        {
            await Patiently.VerifyExpectationAsync(
                () => MessageLock.Received().TryAquireLock(
                    Arg.Is<string>(a => a.Contains(DeserialisedMessage.Id.ToString())),
                    TimeSpan.FromSeconds(_maximumTimeout)));
        }
    }

    [ExactlyOnce]
    public class Handler : IHandler<GenericMessage>
    {
        public bool Handle(GenericMessage message)
        {
            return true;
        }
    }
}