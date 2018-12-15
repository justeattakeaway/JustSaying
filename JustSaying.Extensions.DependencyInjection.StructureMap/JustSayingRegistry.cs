using JustSaying.AwsTools;
using JustSaying.AwsTools.QueueCreation;
using JustSaying.Fluent;
using JustSaying.Messaging.MessageSerialization;
using JustSaying.Messaging.Monitoring;
using StructureMap;

namespace JustSaying
{
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
            For<IMessageSerializationFactory>().Use<NewtonsoftSerializationFactory>().Singleton();
            For<IMessageSubjectProvider>().Use<GenericMessageSubjectProvider>().Singleton();
            For<IVerifyAmazonQueues>().Use<AmazonQueueCreator>().Singleton();

            For<IMessageSerializationRegister>()
                .Use(
                    nameof(IMessageSerializationRegister),
                    (p) =>
                    {
                        var config = p.GetInstance<IMessagingConfig>();
                        return new MessageSerializationRegister(config.MessageSubjectProvider);
                    })
                .Singleton();
        }
    }
}
