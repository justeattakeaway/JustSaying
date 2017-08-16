using System;
using JustSaying.v2.Configuration;

namespace JustSaying.v2.FluentApi
{
    public class AwsPendingMessageSubscriber<TConfiguration> where TConfiguration : IAwsSubscriberConfiguration
    {
        public IAwsMessageSubscriber Builder { get; }
        public Action<TConfiguration> Configuration { get; }

        public AwsPendingMessageSubscriber(IAwsMessageSubscriber builder, Action<TConfiguration> configuration)
        {
            Builder = builder;
            Configuration = configuration;
        }
    }
}