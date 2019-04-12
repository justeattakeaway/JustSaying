using System;
using System.Threading.Tasks;
using JustSaying.Messaging.MessageHandling;
using JustSaying.TestingFramework;
using NSubstitute;
using Shouldly;
using Xunit;

namespace JustSaying.UnitTests.Messaging.MessageHandling
{
    public class WhenEnsuringMessageIsOnlyHandledExactlyOnce
    {
        [Fact]
        public async Task WhenMessageIsLockedByAnotherHandler_MessageWillBeLeftInTheQueue()
        {
            var messageLock = Substitute.For<IMessageLockAsync>();
            messageLock.TryAquireLockAsync(Arg.Any<string>(), Arg.Any<TimeSpan>()).Returns(new MessageLockResponse { DoIHaveExclusiveLock = false });
            var sut = new ExactlyOnceHandler<OrderAccepted>(Substitute.For<IHandlerAsync<OrderAccepted>>(), messageLock, 1, "handlerName");

            var result = await sut.Handle(new OrderAccepted());

            result.ShouldBeFalse();
        }
    }
}
