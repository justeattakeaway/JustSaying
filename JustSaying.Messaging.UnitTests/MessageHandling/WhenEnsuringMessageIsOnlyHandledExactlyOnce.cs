using System;
using System.Threading.Tasks;
using JustSaying.Messaging.MessageHandling;
using JustSaying.TestingFramework;
using NSubstitute;
using NUnit.Framework;

namespace JustSaying.Messaging.UnitTests.MessageHandling
{
    public class WhenEnsuringMessageIsOnlyHandledExactlyOnce
    {
        [Test]
        public async Task WhenMessageIsLockedByAnotherHandler_MessageWillBeLeftInTheQueue()
        {
            var messageLock = Substitute.For<IMessageLock>();
            messageLock.TryAquireLock(Arg.Any<string>(), Arg.Any<TimeSpan>()).Returns(new MessageLockResponse {DoIHaveExclusiveLock = false});
            var sut = new ExactlyOnceHandler<OrderAccepted>(Substitute.For<IAsyncHandler<OrderAccepted>>(), messageLock, 1, "handlerName");

            var result = await sut.Handle(new OrderAccepted());

            Assert.IsFalse(result);
        }
    }
}