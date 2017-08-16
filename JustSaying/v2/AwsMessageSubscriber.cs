using System;
using System.Threading.Tasks;
using JustSaying.Messaging;
using JustSaying.Messaging.MessageHandling;
using JustSaying.Models;
using JustSaying.v2.Configuration;

namespace JustSaying.v2
{
    public interface IAwsMessageSubscriber : IMessageSubscriber
    {
        void Add<TMessage>(AwsTopicSubscriberConfiguration configuration, IHandlerAsync<TMessage> handler) where TMessage : Message;
        void Add<TMessage>(AwsTopicSubscriberConfiguration configuration, IHandlerResolver handlerResolver) where TMessage : Message;
        void Add<TMessage>(AwsQueueSubscriberConfiguration configuration, IHandlerAsync<TMessage> handler) where TMessage : Message;
        void Add<TMessage>(AwsQueueSubscriberConfiguration configuration, IHandlerResolver handlerResolver) where TMessage : Message;
        Task<IMessageSubscriber> BuildAsync();
    }

    public class AwsMessageSubscriber : DeferredActionBuilder, IAwsMessageSubscriber
    {
        public bool Listening { get; private set; }

        public void StartListening()
        {
            Listening = true;
            throw new NotImplementedException();
        }

        public void StopListening()
        {
            Listening = false;
            throw new NotImplementedException();
        }

        public void Add<TMessage>(AwsTopicSubscriberConfiguration configuration, IHandlerAsync<TMessage> handler) where TMessage : Message
        {
            throw new NotImplementedException();
        }

        public void Add<TMessage>(AwsTopicSubscriberConfiguration configuration, IHandlerResolver handlerResolver) where TMessage : Message
        {
            throw new NotImplementedException();
        }

        public void Add<TMessage>(AwsQueueSubscriberConfiguration configuration, IHandlerAsync<TMessage> handler) where TMessage : Message
        {
            throw new NotImplementedException();
        }

        public void Add<TMessage>(AwsQueueSubscriberConfiguration configuration, IHandlerResolver handlerResolver) where TMessage : Message
        {
            throw new NotImplementedException();
        }

        public async Task<IMessageSubscriber> BuildAsync()
        {
            await ExecuteActionsAsync();
            return this;
        }
    }
}