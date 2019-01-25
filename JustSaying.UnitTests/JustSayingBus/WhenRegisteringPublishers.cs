using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using JustSaying.Messaging;
using JustSaying.Models;
using JustSaying.TestingFramework;
using NSubstitute;
using Shouldly;
using Xunit;

namespace JustSaying.UnitTests.JustSayingBus
{
    public class WhenRegisteringPublishers : GivenAServiceBus
    {
        private IMessagePublisher _publisher;

        protected override void Given()
        {
            base.Given();
            _publisher = Substitute.For<IMessagePublisher>();
        }

        protected override async Task WhenAction()
        {
            SystemUnderTest.AddMessagePublisher<OrderAccepted>(_publisher, string.Empty);
            SystemUnderTest.AddMessagePublisher<OrderRejected>(_publisher, string.Empty);

            await SystemUnderTest.PublishAsync(new OrderAccepted());
            await SystemUnderTest.PublishAsync(new OrderRejected());
            await SystemUnderTest.PublishAsync(new OrderRejected());
        }

        [Fact]
        public void AcceptedOrderWasPublishedOnce()
        {
            _publisher.Received(1).PublishAsync(
                Arg.Is<Message>(m => m is OrderAccepted),
                Arg.Any<PublishMetadata>(),
                Arg.Any<CancellationToken>());
        }

        [Fact]
        public void RejectedOrderWasPublishedTwice()
        {
            _publisher.Received(2).PublishAsync(
                Arg.Is<Message>(m => m is OrderRejected),
                Arg.Any<PublishMetadata>(),
                Arg.Any<CancellationToken>());
        }

        [Fact]
        public void AndInterrogationShowsPublishersHaveBeenSet()
        {
            var response = SystemUnderTest.WhatDoIHave();

            response.Publishers.Count().ShouldBe(2);
            response.Publishers.First(x => x.MessageType == typeof(OrderAccepted)).ShouldNotBe(null);
            response.Publishers.First(x => x.MessageType == typeof(OrderRejected)).ShouldNotBe(null);
        }
    }
}
