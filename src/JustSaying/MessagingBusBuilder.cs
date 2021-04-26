using System;
using JustSaying.AwsTools;
using JustSaying.AwsTools.QueueCreation;
using JustSaying.Fluent;
using JustSaying.Messaging;
using JustSaying.Messaging.MessageHandling;
using JustSaying.Messaging.MessageSerialization;
using JustSaying.Messaging.Monitoring;
using Microsoft.Extensions.Logging;

namespace JustSaying
{
    /// <summary>
    /// A class representing a builder for instances of <see cref="IMessagingBus"/>
    /// and <see cref="IMessagePublisher"/>. This class cannot be inherited.
    /// </summary>
    public sealed class MessagingBusBuilder
    {
        /// <summary>
        /// Gets the <see cref="IServiceResolver"/> to use.
        /// </summary>
        internal IServiceResolver ServiceResolver { get; private set; } = new DefaultServiceResolver();

        /// <summary>
        /// Gets or sets the builder to use for services.
        /// </summary>
        internal ServicesBuilder ServicesBuilder { get; private set; }

        /// <summary>
        /// Gets or sets the builder to use for creating an AWS client factory.
        /// </summary>
        private AwsClientFactoryBuilder ClientFactoryBuilder { get; set; }

        /// <summary>
        /// Gets or sets the builder to use to configure messaging.
        /// </summary>
        private MessagingConfigurationBuilder MessagingConfig { get; set; }

        /// <summary>
        /// Gets or sets the builder to use for publications.
        /// </summary>
        private PublicationsBuilder PublicationsBuilder { get; set; }

        /// <summary>
        /// Gets or sets the builder to use for subscriptions.
        /// </summary>
        private SubscriptionsBuilder SubscriptionBuilder { get; set; }

        /// <summary>
        /// Configures the factory for AWS clients.
        /// </summary>
        /// <param name="configure">A delegate to a method to use to configure the AWS clients.</param>
        /// <returns>
        /// The current <see cref="MessagingBusBuilder"/>.
        /// </returns>
        /// <exception cref="ArgumentNullException">
        /// <paramref name="configure"/> is <see langword="null"/>.
        /// </exception>
        public MessagingBusBuilder Client(Action<AwsClientFactoryBuilder> configure)
        {
            if (configure == null)
            {
                throw new ArgumentNullException(nameof(configure));
            }

            if (ClientFactoryBuilder == null)
            {
                ClientFactoryBuilder = new AwsClientFactoryBuilder(this);
            }

            configure(ClientFactoryBuilder);

            return this;
        }

        /// <summary>
        /// Configures messaging.
        /// </summary>
        /// <param name="configure">A delegate to a method to use to configure messaging.</param>
        /// <returns>
        /// The current <see cref="MessagingBusBuilder"/>.
        /// </returns>
        /// <exception cref="ArgumentNullException">
        /// <paramref name="configure"/> is <see langword="null"/>.
        /// </exception>
        public MessagingBusBuilder Messaging(Action<MessagingConfigurationBuilder> configure)
        {
            if (configure == null)
            {
                throw new ArgumentNullException(nameof(configure));
            }

            if (MessagingConfig == null)
            {
                MessagingConfig = new MessagingConfigurationBuilder(this);
            }

            configure(MessagingConfig);

            return this;
        }

        /// <summary>
        /// Configures the publications.
        /// </summary>
        /// <param name="configure">A delegate to a method to use to configure publications.</param>
        /// <returns>
        /// The current <see cref="MessagingBusBuilder"/>.
        /// </returns>
        /// <exception cref="ArgumentNullException">
        /// <paramref name="configure"/> is <see langword="null"/>.
        /// </exception>
        public MessagingBusBuilder Publications(Action<PublicationsBuilder> configure)
        {
            if (configure == null)
            {
                throw new ArgumentNullException(nameof(configure));
            }

            if (PublicationsBuilder == null)
            {
                PublicationsBuilder = new PublicationsBuilder(this);
            }

            configure(PublicationsBuilder);

            return this;
        }

        /// <summary>
        /// Configures the services.
        /// </summary>
        /// <param name="configure">A delegate to a method to use to configure JustSaying services.</param>
        /// <returns>
        /// The current <see cref="MessagingBusBuilder"/>.
        /// </returns>
        /// <exception cref="ArgumentNullException">
        /// <paramref name="configure"/> is <see langword="null"/>.
        /// </exception>
        public MessagingBusBuilder Services(Action<ServicesBuilder> configure)
        {
            if (configure == null)
            {
                throw new ArgumentNullException(nameof(configure));
            }

            if (ServicesBuilder == null)
            {
                ServicesBuilder = new ServicesBuilder(this);
            }

            configure(ServicesBuilder);

            return this;
        }

        /// <summary>
        /// Configures the subscriptions.
        /// </summary>
        /// <param name="configure">A delegate to a method to use to configure subscriptions.</param>
        /// <returns>
        /// The current <see cref="MessagingBusBuilder"/>.
        /// </returns>
        /// <exception cref="ArgumentNullException">
        /// <paramref name="configure"/> is <see langword="null"/>.
        /// </exception>
        public MessagingBusBuilder Subscriptions(Action<SubscriptionsBuilder> configure)
        {
            if (configure == null)
            {
                throw new ArgumentNullException(nameof(configure));
            }

            if (SubscriptionBuilder == null)
            {
                SubscriptionBuilder = new SubscriptionsBuilder(this);
            }

            configure(SubscriptionBuilder);

            return this;
        }

        /// <summary>
        /// Specifies the <see cref="IServiceResolver"/> to use.
        /// </summary>
        /// <param name="serviceResolver">The <see cref="IServiceResolver"/> to use.</param>
        /// <returns>
        /// The current <see cref="MessagingBusBuilder"/>.
        /// </returns>
        /// <exception cref="ArgumentNullException">
        /// <paramref name="serviceResolver"/> is <see langword="null"/>.
        /// </exception>
        public MessagingBusBuilder WithServiceResolver(IServiceResolver serviceResolver)
        {
            ServiceResolver = serviceResolver ?? throw new ArgumentNullException(nameof(serviceResolver));
            return this;
        }

        /// <summary>
        /// Creates a new instance of <see cref="IAwsClientFactory"/>.
        /// </summary>
        /// <returns>
        /// The created instance of <see cref="IAwsClientFactory"/>
        /// </returns>
        public IAwsClientFactory BuildClientFactory()
        {
            return ClientFactoryBuilder?.Build() ?? ServiceResolver.ResolveService<IAwsClientFactory>();
        }

        /// <summary>
        /// Creates a new instance of <see cref="IMessagePublisher"/>.
        /// </summary>
        /// <returns>
        /// The created instance of <see cref="IMessagePublisher"/>
        /// </returns>
        public IMessagePublisher BuildPublisher()
        {
            IMessagingConfig config = CreateConfig();

            config.Validate();

            ILoggerFactory loggerFactory =
                ServicesBuilder?.LoggerFactory?.Invoke() ?? ServiceResolver.ResolveService<ILoggerFactory>();

            JustSayingBus bus = CreateBus(config, loggerFactory);
            IAwsClientFactoryProxy proxy = CreateFactoryProxy();

            if (PublicationsBuilder != null)
            {
                PublicationsBuilder.Configure(bus, proxy, loggerFactory);
            }

            return bus;
        }

        /// <summary>
        /// Creates a new instance of <see cref="IMessagingBus"/>.
        /// </summary>
        /// <returns>
        /// The created instance of <see cref="IMessagingBus"/>
        /// </returns>
        public IMessagingBus BuildSubscribers()
        {
            IMessagingConfig config = CreateConfig();

            config.Validate();

            ILoggerFactory loggerFactory =
                ServicesBuilder?.LoggerFactory?.Invoke() ?? ServiceResolver.ResolveService<ILoggerFactory>();

            JustSayingBus bus = CreateBus(config, loggerFactory);
            IVerifyAmazonQueues creator = CreateQueueCreator(loggerFactory);

            if (ServicesBuilder?.MessageContextAccessor != null)
            {
                bus.MessageContextAccessor = ServicesBuilder.MessageContextAccessor();
            }

            if (SubscriptionBuilder != null)
            {
                SubscriptionBuilder.Configure(bus, ServiceResolver, creator, loggerFactory);
            }

            return bus;
        }

        private JustSayingBus CreateBus(IMessagingConfig config, ILoggerFactory loggerFactory)
        {
            IMessageSerializationRegister register =
                ServicesBuilder?.SerializationRegister?.Invoke() ?? ServiceResolver.ResolveService<IMessageSerializationRegister>();

            var bus =  new JustSayingBus(config, register, loggerFactory);

            bus.Monitor = CreateMessageMonitor();
            bus.MessageContextAccessor = CreateMessageContextAccessor();

            return bus;
        }

        private IMessagingConfig CreateConfig()
        {
            return MessagingConfig != null ?
                MessagingConfig.Build() :
                ServiceResolver.ResolveService<IMessagingConfig>();
        }

        private IAwsClientFactoryProxy CreateFactoryProxy()
        {
            return ClientFactoryBuilder != null ?
                new AwsClientFactoryProxy(new Lazy<IAwsClientFactory>(ClientFactoryBuilder.Build)) :
                ServiceResolver.ResolveService<IAwsClientFactoryProxy>();
        }

        private IMessageMonitor CreateMessageMonitor()
        {
            return ServicesBuilder?.MessageMonitoring?.Invoke() ?? ServiceResolver.ResolveService<IMessageMonitor>();
        }

        private IMessageContextAccessor CreateMessageContextAccessor()
        {
            return ServicesBuilder?.MessageContextAccessor?.Invoke() ?? ServiceResolver.ResolveService<IMessageContextAccessor>();
        }

        private IVerifyAmazonQueues CreateQueueCreator(ILoggerFactory loggerFactory)
        {
            IAwsClientFactoryProxy proxy = CreateFactoryProxy();
            IVerifyAmazonQueues queueCreator = new AmazonQueueCreator(proxy, loggerFactory);

            return queueCreator;
        }
    }
}
