using JustSaying.AwsTools;
using JustSaying.AwsTools.QueueCreation;
using JustSaying.Fluent;
using JustSaying.Messaging.MessageHandling;
using JustSaying.Messaging.MessageSerialization;
using JustSaying.Messaging.Middleware.Logging;
using JustSaying.Messaging.Middleware.PostProcessing;
using JustSaying.Messaging.Monitoring;
using JustSaying.Naming;
using StructureMap;

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
        For<MessagingConfig>().Use<MessagingConfig>().Singleton();
        For<IMessagingConfig>().Use(context => context.GetInstance<MessagingConfig>()).Singleton();
        For<IPublishBatchConfiguration>().Use<MessagingConfig>(context => context.GetInstance<MessagingConfig>()).Singleton();
        For<IMessageMonitor>().Use<NullOpMessageMonitor>().Singleton();
        For<IMessageSerializationFactory>().Use<NewtonsoftSerializationFactory>().Singleton();
        For<IMessageSubjectProvider>().Use<GenericMessageSubjectProvider>().Singleton();
        For<IVerifyAmazonQueues>().Use<AmazonQueueCreator>().Singleton();

        For<MessageContextAccessor>().Use<MessageContextAccessor>().Singleton();
        For<IMessageContextAccessor>().Use(context => context.GetInstance<MessageContextAccessor>());
        For<IMessageContextReader>().Use(context => context.GetInstance<MessageContextAccessor>());

        For<LoggingMiddleware>().Transient();
        For<SqsPostProcessorMiddleware>().Transient();

        For<IMessageSerializationRegister>()
            .Use(
                nameof(IMessageSerializationRegister),
                (p) =>
                {
                    var config = p.GetInstance<IMessagingConfig>();
                    var serializerFactory = p.GetInstance<IMessageSerializationFactory>();
                    return new MessageSerializationRegister(config.MessageSubjectProvider, serializerFactory);
                })
            .Singleton();

        For<DefaultNamingConventions>().Singleton();
        For<ITopicNamingConvention>().Use(context => context.GetInstance<DefaultNamingConventions>());
        For<IQueueNamingConvention>().Use(context => context.GetInstance<DefaultNamingConventions>());
    }
}
