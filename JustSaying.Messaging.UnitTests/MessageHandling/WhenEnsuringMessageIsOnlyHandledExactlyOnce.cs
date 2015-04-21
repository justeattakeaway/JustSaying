using System;
using JustSaying.Messaging.MessageHandling;
using JustSaying.TestingFramework;
using NSubstitute;
using NUnit.Framework;

namespace JustSaying.Messaging.UnitTests.MessageHandling
{
    public class WhenEnsuringMessageIsOnlyHandledExactlyOnce
    {
        [Test]
        public void WhenMessageIsLockedByAnotherHandler_MessageWillBeLeftInTheQueue()
        {
            var messageLock = Substitute.For<IMessageLock>();
            messageLock.TryAquireLock(Arg.Any<string>(), Arg.Any<TimeSpan>()).Returns(new MessageLockResponse(){DoIHaveExclusiveLock = false});
            var sut = new ExactlyOnceHandler<OrderAccepted>(Substitute.For<IHandler<OrderAccepted>>(), messageLock, 1);

            var result = sut.Handle(new OrderAccepted());

            Assert.IsFalse(result);
        }
    }
}