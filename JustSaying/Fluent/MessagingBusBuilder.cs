using System;
using JustSaying.AwsTools.QueueCreation;
using JustSaying.Messaging.MessageSerialisation;
using Microsoft.Extensions.Logging;

namespace JustSaying.Fluent
{
    /// <summary>
    /// A class representing a builder for an <see cref="IMessagingBus"/>.
    /// </summary>
    public class MessagingBusBuilder : Builder<IMessagingBus, MessagingBusBuilder>
    {
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
        /// <returns>
        /// The <see cref="AwsClientFactoryBuilder"/> to use to configure the client.
        /// </returns>
        public AwsClientFactoryBuilder Client()
        {
            if (ClientFactoryBuilder == null)
            {
                ClientFactoryBuilder = new AwsClientFactoryBuilder(this);
            }

            return ClientFactoryBuilder;
        }

        /// <summary>
        /// Configures messaging.
        /// </summary>
        /// <returns>
        /// The <see cref="MessagingConfigurationBuilder"/> to use to configure messaging.
        /// </returns>
        public MessagingConfigurationBuilder Messaging()
        {
            if (MessagingConfig == null)
            {
                MessagingConfig = new MessagingConfigurationBuilder(this);
            }

            return MessagingConfig;
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
        /// Creates a new instance of <see cref="IMessagingBus"/>.
        /// </summary>
        /// <returns>
        /// The created instance of <see cref="IMessagingBus"/>
        /// </returns>
        public override IMessagingBus Build()
        {
            IMessagingConfig config = Messaging().Build();
            config.Validate();

            IMessageSerialisationRegister register = SerializationRegister?.Invoke() ?? new MessageSerialisationRegister(config.MessageSubjectProvider);
            ILoggerFactory loggerFactory = LoggerFactory?.Invoke();

            var bus = new JustSayingBus(config, register, loggerFactory);

            // TODO Remove the need to use the old fluent interface
            // TODO Provide a way to configure these via this builder if needed?
            var proxy = new AwsTools.AwsClientFactoryProxy(Client().Build);
            var queueCreator = new AmazonQueueCreator(proxy, loggerFactory);
            var fluent = new JustSayingFluently(bus, queueCreator, proxy, loggerFactory);

            if (NamingStrategy != null)
            {
                fluent.WithNamingStrategy(NamingStrategy);
            }

            // TODO Subscriptions, handlers and publishers

            return bus;
        }
    }
}
