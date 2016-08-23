using System.Linq;
using System.Threading.Tasks;
using JustBehave;
using JustSaying.Messaging;
using JustSaying.Models;
using NSubstitute;
using NUnit.Framework;
using Shouldly;

namespace JustSaying.UnitTests.JustSayingBus
{
    public class WhenRegisteringTheSamePublisherTwice : GivenAServiceBus
    {
        private IMessagePublisher _publisher;

        protected override void Given()
        {
            base.Given();
            _publisher = Substitute.For<IMessagePublisher>();
            RecordAnyExceptionsThrown();
        }

        protected override Task When()
        {
            SystemUnderTest.AddMessagePublisher<Message>(_publisher, string.Empty);
            SystemUnderTest.AddMessagePublisher<Message>(_publisher, string.Empty);
            return Task.FromResult(true);
        }

        [Then]
        public void NoExceptionIsThrown()
        {
            // Specifying failover regions mean that messages can be registered more than once.
            Assert.Null(ThrownException);
        }

        [Then]
        public void AndInterrogationShowsNonDuplicatedPublishers()
        {
            var response = SystemUnderTest.WhatDoIHave();

            response.Publishers.Count().ShouldBe(1);
            response.Publishers.First(x => x.MessageType == typeof(Message)).ShouldNotBe(null);
        } 
    }
}