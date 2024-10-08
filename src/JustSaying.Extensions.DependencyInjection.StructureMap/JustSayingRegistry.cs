using JustSaying.AwsTools;
using JustSaying.AwsTools.QueueCreation;
using JustSaying.Fluent;
using JustSaying.Messaging.Channels.Receive;
using JustSaying.Messaging.Compression;
using JustSaying.Messaging.MessageHandling;
using JustSaying.Messaging.MessageSerialization;
using JustSaying.Messaging.Middleware.Logging;
using JustSaying.Messaging.Middleware.PostProcessing;
using JustSaying.Messaging.Monitoring;
using JustSaying.Naming;
using StructureMap;
using StructureMap.Pipeline;

namespace JustSaying;

/// <summary>
/// A class representing a StructureMap registry for JustSaying services.
/// </summary>
internal sealed class JustSayingRegistry : Registry
{
    /// <summary>
    /// Initializes a new instance of the <see cref="JustSayingRegistry"/> class.
    /// </summary>
    public JustSayingRegistry()
    {
        // Register as self so the same singleton instance implements two different interfaces
        For<ContextResolver>().Use((p) => new ContextResolver(p)).Singleton();
        For<IHandlerResolver>().Use((p) => p.GetInstance<ContextResolver>()).Singleton();
        For<IServiceResolver>().Use((p) => p.GetInstance<ContextResolver>()).Singleton();

        For<IAwsClientFactory>().Use<DefaultAwsClientFactory>().Singleton();
        For<IAwsClientFactoryProxy>().Use((p) => new AwsClientFactoryProxy(p.GetInstance<IAwsClientFactory>)).Singleton();
        For<IMessagingConfig>().Use<MessagingConfig>().Singleton();
        For<IMessageMonitor>().Use<NullOpMessageMonitor>().Singleton();
        For<IMessageBodySerializationFactory>().Use<NewtonsoftSerializationFactory>().Singleton();
        For<IMessageSubjectProvider>().Use<GenericMessageSubjectProvider>().Singleton();
        For<IVerifyAmazonQueues>().Use<AmazonQueueCreator>().Singleton();

        For<MessageContextAccessor>().Use<MessageContextAccessor>().Singleton();
        For<IMessageContextAccessor>().Use(context => context.GetInstance<MessageContextAccessor>());
        For<IMessageContextReader>().Use(context => context.GetInstance<MessageContextAccessor>());

        For<LoggingMiddleware>().Transient();
        For<SqsPostProcessorMiddleware>().Transient();
        For<IMessageBodyCompression>().Add<GzipMessageBodyCompression>().Singleton();
        For<MessageCompressionRegistry>().Singleton();
        For<IMessageReceivePauseSignal>().Use<MessageReceivePauseSignal>().Singleton();

        For<DefaultNamingConventions>().Singleton();
        For<ITopicNamingConvention>().Use(context => context.GetInstance<DefaultNamingConventions>());
        For<IQueueNamingConvention>().Use(context => context.GetInstance<DefaultNamingConventions>());
    }
}
