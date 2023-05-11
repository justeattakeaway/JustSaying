using JustSaying.AwsTools;
using JustSaying.AwsTools.QueueCreation;
using JustSaying.Fluent;
using JustSaying.Fluent.ServiceResolver;
using JustSaying.Messaging;
using JustSaying.Messaging.Channels.Receive;
using JustSaying.Messaging.MessageSerialization;
using JustSaying.Messaging.Monitoring;
using Microsoft.Extensions.Logging;

namespace JustSaying;

/// <summary>
/// A class representing a builder for instances of <see cref="IMessagingBus"/>
/// and <see cref="IMessagePublisher"/>. This class cannot be inherited.
/// </summary>
public sealed class MessagingBusBuilder
{
    /// <summary>
    /// Gets the <see cref="IServiceResolver"/> to use.
    /// </summary>
    internal IServiceResolver ServiceResolver { get; private set; }

    /// <summary>
    /// Gets or sets the builder to use for services.
    /// </summary>
    internal ServicesBuilder ServicesBuilder { get; private set; }

    /// <summary>
    /// Gets or sets the builder to use for creating an AWS client factory.
    /// </summary>
    internal AwsClientFactoryBuilder ClientFactoryBuilder { get; set; }

    /// <summary>
    /// Gets or sets the builder to use to configure messaging.
    /// </summary>
    internal MessagingConfigurationBuilder MessagingConfig { get; set; }

    /// <summary>
    /// Gets or sets the builder to use for publications.
    /// </summary>
    private PublicationsBuilder PublicationsBuilder { get; set; }

    /// <summary>
    /// Gets or sets the builder to use for subscriptions.
    /// </summary>
    private SubscriptionsBuilder SubscriptionBuilder { get; set; }

    /// <summary>
    /// Provides an <see cref="IServiceResolver"/> interface over the <see cref="ServicesBuilder"/> builder
    /// so that services can be obtained in a consistent way
    /// </summary>
    private ServiceBuilderServiceResolver ServiceBuilderServiceResolver { get; set; }

    public MessagingBusBuilder()
    {
        ServicesBuilder = new ServicesBuilder(this);
        ServiceBuilderServiceResolver = new ServiceBuilderServiceResolver(ServicesBuilder);
        ServiceResolver =
            new CompoundServiceResolver(ServiceBuilderServiceResolver, new DefaultServiceResolver());
        SubscriptionBuilder = new SubscriptionsBuilder(this);
        MessagingConfig = new MessagingConfigurationBuilder(this);
    }

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
        if (serviceResolver == null) throw new ArgumentNullException(nameof(serviceResolver));

        ServiceResolver = new CompoundServiceResolver(ServiceBuilderServiceResolver, serviceResolver);
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
        IMessagingConfig config = MessagingConfig.Build();

        config.Validate();

        ILoggerFactory loggerFactory = ServiceResolver.ResolveService<ILoggerFactory>();

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
        IMessagingConfig config = MessagingConfig.Build();

        config.Validate();

        ILoggerFactory loggerFactory = ServiceResolver.ResolveService<ILoggerFactory>();

        JustSayingBus bus = CreateBus(config, loggerFactory);
        IAwsClientFactoryProxy proxy = CreateFactoryProxy();
        IVerifyAmazonQueues creator = new AmazonQueueCreator(proxy, loggerFactory);

        SubscriptionBuilder.Configure(bus, ServiceResolver, creator, proxy, loggerFactory);

        return bus;
    }

    private JustSayingBus CreateBus(IMessagingConfig config, ILoggerFactory loggerFactory)
    {
        IMessageSerializationRegister register = ServiceResolver.ResolveService<IMessageSerializationRegister>();
        IMessageReceivePauseSignal messageReceivePauseSignal = ServiceResolver.ResolveService<IMessageReceivePauseSignal>();
        IMessageMonitor monitor = ServiceResolver.ResolveOptionalService<IMessageMonitor>() ?? new NullOpMessageMonitor();

        var bus = new JustSayingBus(config, register, messageReceivePauseSignal, loggerFactory, monitor);

        return bus;
    }

    private IAwsClientFactoryProxy CreateFactoryProxy()
    {
        return ClientFactoryBuilder != null ?
            new AwsClientFactoryProxy(new Lazy<IAwsClientFactory>(ClientFactoryBuilder.Build)) :
            ServiceResolver.ResolveService<IAwsClientFactoryProxy>();
    }
}
