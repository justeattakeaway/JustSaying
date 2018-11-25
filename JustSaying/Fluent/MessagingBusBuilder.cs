using System;
using JustSaying.AwsTools;
using JustSaying.AwsTools.QueueCreation;
using JustSaying.Messaging.MessageSerialisation;
using JustSaying.Messaging.Monitoring;
using Microsoft.Extensions.Logging;

namespace JustSaying.Fluent
{
    /// <summary>
    /// A class representing a builder for an <see cref="IMessagingBus"/>.
    /// </summary>
    public class MessagingBusBuilder : Builder<IMessagingBus, MessagingBusBuilder>
    {
        /// <summary>
        /// Gets the optional <see cref="IServiceResolver"/> to use.
        /// </summary>
        internal IServiceResolver ServiceResolver { get; private set; }

        /// <inheritdoc />
        protected override MessagingBusBuilder Self => this;

        /// <summary>
        /// Gets or sets the builder to use for creating an AWS client factory.
        /// </summary>
        private AwsClientFactoryBuilder ClientFactoryBuilder { get; set; }

        /// <summary>
        /// Gets or sets the builder to use to configure messaging.
        /// </summary>
        private MessagingConfigurationBuilder MessagingConfig { get; set; }

        /// <summary>
        /// Gets or sets the builder to use for subscriptions.
        /// </summary>
        private SubscriptionBuilder SubscriptionBuilder { get; set; }

        /// <summary>
        /// Gets or sets a delegate to a method to create the <see cref="ILoggerFactory"/> to use.
        /// </summary>
        private Func<ILoggerFactory> LoggerFactory { get; set; }

        /// <summary>
        /// Gets or sets a delegate to a method to create the <see cref="INamingStrategy"/> to use.
        /// </summary>
        private Func<INamingStrategy> NamingStrategy { get; set; }

        /// <summary>
        /// Gets or sets a delegate to a method to create the <see cref="IMessageSerialisationRegister"/> to use.
        /// </summary>
        private Func<IMessageSerialisationRegister> SerializationRegister { get; set; }

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

            return Self;
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

            return Self;
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
        public MessagingBusBuilder Subscriptions(Action<SubscriptionBuilder> configure)
        {
            if (configure == null)
            {
                throw new ArgumentNullException(nameof(configure));
            }

            if (SubscriptionBuilder == null)
            {
                SubscriptionBuilder = new SubscriptionBuilder(this);
            }

            configure(SubscriptionBuilder);

            return Self;
        }

        /// <summary>
        /// Specifies the <see cref="ILoggerFactory"/> to use.
        /// </summary>
        /// <param name="loggerFactory">The <see cref="ILoggerFactory"/> to use.</param>
        /// <returns>
        /// The current <see cref="MessagingBusBuilder"/>.
        /// </returns>
        public MessagingBusBuilder WithLoggerFactory(ILoggerFactory loggerFactory)
            => WithLoggerFactory(() => loggerFactory);

        /// <summary>
        /// Specifies the <see cref="ILoggerFactory"/> to use.
        /// </summary>
        /// <param name="loggerFactory">A delegate to a method to get the <see cref="ILoggerFactory"/> to use.</param>
        /// <returns>
        /// The current <see cref="MessagingBusBuilder"/>.
        /// </returns>
        public MessagingBusBuilder WithLoggerFactory(Func<ILoggerFactory> loggerFactory)
        {
            LoggerFactory = loggerFactory;
            return this;
        }

        /// <summary>
        /// Specifies the <see cref="INamingStrategy"/> to use.
        /// </summary>
        /// <param name="strategy">The <see cref="INamingStrategy"/> to use.</param>
        /// <returns>
        /// The current <see cref="MessagingBusBuilder"/>.
        /// </returns>
        public MessagingBusBuilder WithNamingStrategy(INamingStrategy strategy)
            => WithNamingStrategy(() => strategy);

        /// <summary>
        /// Specifies the <see cref="INamingStrategy"/> to use.
        /// </summary>
        /// <param name="strategy">A delegate to a method to get the <see cref="INamingStrategy"/> to use.</param>
        /// <returns>
        /// The current <see cref="MessagingBusBuilder"/>.
        /// </returns>
        public MessagingBusBuilder WithNamingStrategy(Func<INamingStrategy> strategy)
        {
            NamingStrategy = strategy;
            return this;
        }

        /// <summary>
        /// Specifies the <see cref="IServiceResolver"/> to use.
        /// </summary>
        /// <param name="serviceResolver">The <see cref="IServiceResolver"/> to use.</param>
        /// <returns>
        /// The current <see cref="MessagingBusBuilder"/>.
        /// </returns>
        public MessagingBusBuilder WithServiceResolver(IServiceResolver serviceResolver)
        {
            ServiceResolver = serviceResolver;
            return Self;
        }

        /// <summary>
        /// Creates a new instance of <see cref="IMessagingBus"/>.
        /// </summary>
        /// <returns>
        /// The created instance of <see cref="IMessagingBus"/>
        /// </returns>
        public override IMessagingBus Build()
        {
            IMessagingConfig config = CreateConfig();

            config.Validate();

            ILoggerFactory loggerFactory =
                LoggerFactory?.Invoke() ?? ServiceResolver?.ResolveService<ILoggerFactory>() ?? new NullLoggerFactory();

            JustSayingBus bus = CreateBus(config, loggerFactory);
            JustSayingFluently fluent = CreateFluent(bus, loggerFactory);

            if (NamingStrategy != null)
            {
                fluent.WithNamingStrategy(NamingStrategy);
            }

            // TODO Publishers
            // TODO Where do topic/queue names come in?
            if (SubscriptionBuilder != null)
            {
                SubscriptionBuilder.Configure(fluent);
            }

            return bus;
        }

        private JustSayingBus CreateBus(IMessagingConfig config, ILoggerFactory loggerFactory)
        {
            IMessageSerialisationRegister register =
                SerializationRegister?.Invoke() ?? ServiceResolver?.ResolveService<IMessageSerialisationRegister>() ?? new MessageSerialisationRegister(config.MessageSubjectProvider);

            return new JustSayingBus(config, register, loggerFactory);
        }

        private IMessagingConfig CreateConfig()
        {
            return MessagingConfig != null ?
                MessagingConfig.Build() :
                ServiceResolver?.ResolveService<IMessagingConfig>() ?? new MessagingConfig();
        }

        private IAwsClientFactoryProxy CreateFactoryProxy()
        {
            return ClientFactoryBuilder != null ?
                new AwsClientFactoryProxy(new Lazy<IAwsClientFactory>(ClientFactoryBuilder.Build)) :
                ServiceResolver?.ResolveService<IAwsClientFactoryProxy>() ?? new AwsClientFactoryProxy();
        }

        private JustSayingFluently CreateFluent(JustSayingBus bus, ILoggerFactory loggerFactory)
        {
            IAwsClientFactoryProxy proxy = CreateFactoryProxy();
            IVerifyAmazonQueues queueCreator = new AmazonQueueCreator(proxy, loggerFactory);

            var fluent = new JustSayingFluently(bus, queueCreator, proxy, loggerFactory);

            IMessageSerialisationFactory serializationFactory = ServiceResolver?.ResolveService<IMessageSerialisationFactory>() ?? new NewtonsoftSerialisationFactory();
            IMessageMonitor messageMonitor = ServiceResolver?.ResolveService<IMessageMonitor>() ?? new NullOpMessageMonitor();

            fluent.WithSerialisationFactory(serializationFactory)
                  .WithMonitoring(messageMonitor);

            return fluent;
        }

        private sealed class NullLoggerFactory : ILoggerFactory
        {
            public void AddProvider(ILoggerProvider provider)
            {
            }

            public ILogger CreateLogger(string categoryName)
            {
                return new NullLogger();
            }

            public void Dispose()
            {
            }

            private sealed class NullLogger : ILogger
            {
                public IDisposable BeginScope<TState>(TState state)
                {
                    return null;
                }

                public bool IsEnabled(LogLevel logLevel)
                {
                    return false;
                }

                public void Log<TState>(LogLevel logLevel, EventId eventId, TState state, Exception exception, Func<TState, Exception, string> formatter)
                {
                }
            }
        }
    }
}
