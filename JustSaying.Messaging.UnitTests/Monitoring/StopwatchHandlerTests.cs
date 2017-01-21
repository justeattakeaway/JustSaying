using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using JustSaying.Messaging.MessageHandling;
using JustSaying.Messaging.Monitoring;
using JustSaying.TestingFramework;
using NSubstitute;
using NUnit.Framework;

namespace JustSaying.Messaging.UnitTests.Monitoring
{
    [TestFixture]
    public class StopwatchHandlerTests
    {
        [Test]
        public async Task WhenHandlerIsWrappedinStopWatch_InnerHandlerIsCalled()
        {
            var handler = MockHandler();
            var monitoring = Substitute.For<IMeasureHandlerExecutionTime>();

            var stopWatchHandler = new StopwatchHandler<OrderAccepted>(handler, monitoring);

            var result = await stopWatchHandler.Handle(new OrderAccepted());
            Assert.That(result, Is.True);

            await handler.Received(1).Handle(Arg.Any<OrderAccepted>());
        }

        [Test]
        public async Task WhenHandlerIsWrappedinStopWatch_MonitoringIsCalled()
        {
            var handler = MockHandler();
            var monitoring = Substitute.For<IMeasureHandlerExecutionTime>();

            var stopWatchHandler = new StopwatchHandler<OrderAccepted>(handler, monitoring);

            await stopWatchHandler.Handle(new OrderAccepted());

            monitoring.Received(1). HandlerExecutionTime(
                Arg.Any<string>(), Arg.Any<string>(), Arg.Any<TimeSpan>());
        }

        [Test]
        public async Task WhenHandlerIsWrappedinStopWatch_MonitoringIsCalledWithCorrectTypeNames()
        {
            var handler = MockHandler();
            var innnerHandlerName = handler.GetType().Name.ToLower();

            var monitoring = Substitute.For<IMeasureHandlerExecutionTime>();

            var stopWatchHandler = new StopwatchHandler<OrderAccepted>(handler, monitoring);

            await stopWatchHandler.Handle(new OrderAccepted());

            monitoring.Received(1).HandlerExecutionTime(
                innnerHandlerName, "orderaccepted", Arg.Any<TimeSpan>());
        }

        [Test]
        public async Task WhenSyncHandlerIsWrappedInEverything_MonitoringIsCalledWithCorrectTypeNames()
        {
            var innerHandler = MockSyncHandler();
            var innnerHandlerName = innerHandler.GetType().Name.ToLower();

            var wrappedHandler1 = new BlockingHandler<OrderAccepted>(innerHandler);
            var wrappedHandler2 = new ExactlyOnceHandler<OrderAccepted>(wrappedHandler1, MockMessageLock(), 100, "test");
            var wrappedHandler3 = new ListHandler<OrderAccepted>(
                new List<IHandlerAsync<OrderAccepted>> { wrappedHandler2 });
            var wrappedHandler4 = new FutureHandler<OrderAccepted>(() => wrappedHandler3);

            var monitoring = Substitute.For<IMeasureHandlerExecutionTime>();
            var stopwatchHandler = new StopwatchHandler<OrderAccepted>(wrappedHandler4, monitoring);


            await stopwatchHandler.Handle(new OrderAccepted());

            monitoring.Received(1).HandlerExecutionTime(
                innnerHandlerName, "orderaccepted", Arg.Any<TimeSpan>());
        }

        private static IMessageLock MockMessageLock()
        {
            var messageLock = Substitute.For<IMessageLock>();
            messageLock.TryAquireLock(Arg.Any<string>(), Arg.Any<TimeSpan>())
                .Returns(new MessageLockResponse());
            return messageLock;
        }

        [Test]
        public async Task WhenHandlerIsWrappedinStopWatch_MonitoringIsCalledWithTiming()
        {
            var handler = MockHandler();
            var monitoring = Substitute.For<IMeasureHandlerExecutionTime>();

            var stopWatchHandler = new StopwatchHandler<OrderAccepted>(handler, monitoring);

            await stopWatchHandler.Handle(new OrderAccepted());

            monitoring.Received(1).HandlerExecutionTime(
                Arg.Any<string>(), Arg.Any<string>(), 
                Arg.Is<TimeSpan>(ts => ts > TimeSpan.Zero));
        }

        private static IHandlerAsync<OrderAccepted> MockHandler()
        {
            var handler = Substitute.For<IHandlerAsync<OrderAccepted>>();
            handler.Handle(Arg.Any<OrderAccepted>())
                .Returns(Task.FromResult(true));
            return handler;
        }

        private static IHandler<OrderAccepted> MockSyncHandler()
        {
            var handler = Substitute.For<IHandler<OrderAccepted>>();
            handler.Handle(Arg.Any<OrderAccepted>())
                .Returns(true);
            return handler;
        }
    }
}
