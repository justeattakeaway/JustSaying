using System;
using System.Threading.Tasks;
using JustSaying.Messaging.MessageHandling;
using JustSaying.TestingFramework;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;
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
            // Arrange
            var messageLock = Substitute.For<IMessageLockAsync>();

            messageLock
                .TryAquireLockAsync(Arg.Any<string>(), Arg.Any<TimeSpan>())
                .Returns(new MessageLockResponse { DoIHaveExclusiveLock = false });

            var logger = NullLoggerFactory.Instance.CreateLogger<ExactlyOnceHandler<OrderAccepted>>();

            var sut = new ExactlyOnceHandler<OrderAccepted>(
                Substitute.For<IHandlerAsync<OrderAccepted>>(),
                messageLock,
                TimeSpan.FromSeconds(1),
                "handlerName",
                logger);

            // Act
            var result = await sut.Handle(new OrderAccepted());

            // Assert
            result.ShouldBeFalse();
        }
    }
}
