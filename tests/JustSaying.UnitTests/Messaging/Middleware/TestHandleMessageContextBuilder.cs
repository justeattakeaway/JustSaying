using System;
using Amazon.SQS.Model;
using JustSaying.Messaging.Channels.Context;
using JustSaying.Messaging.MessageHandling;
using JustSaying.Messaging.Middleware;
using JustSaying.UnitTests.Messaging.Channels.Fakes;

namespace JustSaying.UnitTests.Messaging.Middleware
{
    public static class TestHandleContexts
    {
        public static HandleMessageContext From<TMessage>(TMessage message = default, string queueName = null)
        where TMessage : JustSaying.Models.Message, new()
        {
            return new HandleMessageContext(
                queueName ?? "test-queue",
                new Message(),
                message ?? new TMessage(),
                typeof(TMessage),
                new FakeVisbilityUpdater(),
                new FakeMessageDeleter(),
                new Uri("http://test-queue"),
                new MessageAttributes());
        }
    }
}
