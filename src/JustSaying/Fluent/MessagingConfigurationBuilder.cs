using Amazon;
using JustSaying.AwsTools.MessageHandling;
using JustSaying.Messaging.MessageSerialization;
using JustSaying.Models;
using JustSaying.Naming;

namespace JustSaying.Fluent
{
    /// <summary>
    /// A class representing a builder for instances of <see cref="IMessagingConfig"/>. This class cannot be inherited.
    /// </summary>
    public sealed class MessagingConfigurationBuilder
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="MessagingConfigurationBuilder"/> class.
        /// </summary>
        /// <param name="busBuilder">The <see cref="MessagingBusBuilder"/> that owns this instance.</param>
        internal MessagingConfigurationBuilder(MessagingBusBuilder busBuilder)
        {
            BusBuilder = busBuilder;
        }

        public MessagingBusBuilder BusBuilder { get; }

        /// <summary>
        /// Gets or sets the optional value to use for <see cref="IPublishConfiguration.AdditionalSubscriberAccounts"/>
        /// </summary>
        private List<string> AdditionalSubscriberAccounts { get; set; }

        /// <summary>
        /// Gets or sets the optional value to use for <see cref="IPublishConfiguration.MessageResponseLogger"/>
        /// </summary>
        private Action<MessageResponse, Message> MessageResponseLogger { get; set; }

        /// <summary>
        /// Gets or sets the optional value to use for <see cref="IPublishConfiguration.PublishFailureBackoff"/>
        /// </summary>
        private TimeSpan? PublishFailureBackoff { get; set; }

        /// <summary>
        /// Gets or sets the optional value to use for <see cref="IPublishConfiguration.PublishFailureReAttempts"/>
        /// </summary>
        private int? PublishFailureReAttempts { get; set; }

        /// <summary>
        /// Gets or sets the optional value to use for <see cref="IMessagingConfig.Region"/>
        /// </summary>
        private string Region { get; set; }

        /// <summary>
        /// Gets or sets the optional value to use for <see cref="IMessagingConfig.MessageSubjectProvider"/>
        /// </summary>
        private IMessageSubjectProvider MessageSubjectProvider { get; set; }

        /// <summary>
        /// Gets or sets the optional value to use for <see cref="IMessagingConfig.TopicNamingConvention"/>
        /// </summary>
        private ITopicNamingConvention TopicNamingConvention { get; set; }

        /// <summary>
        /// Gets or sets the optional value to use for <see cref="IMessagingConfig.QueueNamingConvention"/>
        /// </summary>
        private IQueueNamingConvention QueueNamingConvention { get; set; }


        /// <summary>
        /// Specifies additional subscriber account(s) to use.
        /// </summary>
        /// <param name="regions">The AWS account Id(s) to additionally subscribe to.</param>
        /// <returns>
        /// The current <see cref="MessagingConfigurationBuilder"/>.
        /// </returns>
        /// <exception cref="ArgumentNullException">
        /// <paramref name="regions"/> is <see langword="null"/>.
        /// </exception>
        public MessagingConfigurationBuilder WithAdditionalSubscriberAccounts(params string[] regions)
            => WithAdditionalSubscriberAccounts(regions as IEnumerable<string>);

        /// <summary>
        /// Specifies additional subscriber account(s) to use.
        /// </summary>
        /// <param name="accountIds">The AWS account Id(s) to additionally subscribe to.</param>
        /// <returns>
        /// The current <see cref="MessagingConfigurationBuilder"/>.
        /// </returns>
        /// <exception cref="ArgumentNullException">
        /// <paramref name="accountIds"/> is <see langword="null"/>.
        /// </exception>
        public MessagingConfigurationBuilder WithAdditionalSubscriberAccounts(IEnumerable<string> accountIds)
        {
            if (accountIds == null)
            {
                throw new ArgumentNullException(nameof(accountIds));
            }

            AdditionalSubscriberAccounts = new List<string>(accountIds);
            return this;
        }

        /// <summary>
        /// Specifies an additional subscriber account to use.
        /// </summary>
        /// <param name="accountId">The AWS account Id to additionally subscribe to.</param>
        /// <returns>
        /// The current <see cref="MessagingConfigurationBuilder"/>.
        /// </returns>
        /// <exception cref="ArgumentNullException">
        /// <paramref name="accountId"/> is <see langword="null"/>.
        /// </exception>
        public MessagingConfigurationBuilder WithAdditionalSubscriberAccount(string accountId)
        {
            if (accountId == null)
            {
                throw new ArgumentNullException(nameof(accountId));
            }

            if (AdditionalSubscriberAccounts == null)
            {
                AdditionalSubscriberAccounts = new List<string>();
            }

            AdditionalSubscriberAccounts.Add(accountId);
            return this;
        }

        /// <summary>
        /// Specifies a delegate to use to log message responses.
        /// </summary>
        /// <param name="logger">A delegate to a method to use to log message responses.</param>
        /// <returns>
        /// The current <see cref="MessagingConfigurationBuilder"/>.
        /// </returns>
        /// <exception cref="ArgumentNullException">
        /// <paramref name="logger"/> is <see langword="null"/>.
        /// </exception>
        public MessagingConfigurationBuilder WithMessageResponseLogger(Action<MessageResponse, Message> logger)
        {
            MessageResponseLogger = logger ?? throw new ArgumentNullException(nameof(logger));
            return this;
        }

        /// <summary>
        /// Specifies the <see cref="IMessageSubjectProvider"/> to use.
        /// </summary>
        /// <param name="subjectProvider">The <see cref="IMessageSubjectProvider"/> to use.</param>
        /// <returns>
        /// The current <see cref="MessagingConfigurationBuilder"/>.
        /// </returns>
        /// <exception cref="ArgumentNullException">
        /// <paramref name="subjectProvider"/> is <see langword="null"/>.
        /// </exception>
        public MessagingConfigurationBuilder WithMessageSubjectProvider(IMessageSubjectProvider subjectProvider)
        {
            MessageSubjectProvider = subjectProvider ?? throw new ArgumentNullException(nameof(subjectProvider));
            return this;
        }

        /// <summary>
        /// Specifies the back-off period to use if message publishing fails.
        /// </summary>
        /// <param name="value">The back-off period to use.</param>
        /// <returns>
        /// The current <see cref="MessagingConfigurationBuilder"/>.
        /// </returns>
        public MessagingConfigurationBuilder WithPublishFailureBackoff(TimeSpan value)
        {
            PublishFailureBackoff = value;
            return this;
        }

        /// <summary>
        /// Specifies the number of publish re-attempts to use if message publishing fails.
        /// </summary>
        /// <param name="value">The number of re-attempts.</param>
        /// <returns>
        /// The current <see cref="MessagingConfigurationBuilder"/>.
        /// </returns>
        public MessagingConfigurationBuilder WithPublishFailureReattempts(int value)
        {
            PublishFailureReAttempts = value;
            return this;
        }


        /// <summary>
        /// Specifies an AWS region to use.
        /// </summary>
        /// <param name="region">The AWS region to use.</param>
        /// <returns>
        /// The current <see cref="MessagingConfigurationBuilder"/>.
        /// </returns>
        /// <exception cref="ArgumentNullException">
        /// <paramref name="region"/> is <see langword="null"/>.
        /// </exception>
        public MessagingConfigurationBuilder WithRegion(string region)
        {
            Region = region ?? throw new ArgumentNullException(nameof(region));

            return this;
        }

        /// <summary>
        /// Specifies an AWS region to use.
        /// </summary>
        /// <param name="region">The AWS region to use.</param>
        /// <returns>
        /// The current <see cref="MessagingConfigurationBuilder"/>.
        /// </returns>
        /// <exception cref="ArgumentNullException">
        /// <paramref name="region"/> is <see langword="null"/>.
        /// </exception>
        public MessagingConfigurationBuilder WithRegion(RegionEndpoint region)
        {
            if (region == null)
            {
                throw new ArgumentNullException(nameof(region));
            }

            return WithRegion(region.SystemName);
        }


        /// <summary>
        /// Specifies the <see cref="ITopicNamingConvention"/> to use.
        /// </summary>
        /// <param name="namingConvention">The <see cref="ITopicNamingConvention"/> to use.</param>
        /// <returns>
        /// The current <see cref="MessagingConfigurationBuilder"/>.
        /// </returns>
        /// <exception cref="ArgumentNullException">
        /// <paramref name="namingConvention"/> is <see langword="null"/>.
        /// </exception>
        public MessagingConfigurationBuilder WithTopicNamingConvention(ITopicNamingConvention namingConvention)
        {
            TopicNamingConvention = namingConvention ?? throw new ArgumentNullException(nameof(namingConvention));
            return this;
        }

        /// <summary>
        /// Specifies the <see cref="ITopicNamingConvention"/> to use.
        /// </summary>
        /// <typeparam name="T">The <see cref="ITopicNamingConvention"/> to use which will be resolved from the <see cref="IServiceResolver"/></typeparam>
        /// <returns>
        /// The current <see cref="MessagingConfigurationBuilder"/>.
        /// </returns>
        /// <exception cref="ArgumentNullException">
        /// <typeparamref name="T"/> is resolved to <see langword="null"/>.
        /// </exception>
        public MessagingConfigurationBuilder WithTopicNamingConvention<T>() where T : class, ITopicNamingConvention
        {
            return WithTopicNamingConvention(BusBuilder.ServiceResolver.ResolveService<T>());
        }

        /// <summary>
        /// Specifies the <see cref="IQueueNamingConvention"/> to use.
        /// </summary>
        /// <param name="namingConvention">The <see cref="IQueueNamingConvention"/> to use.</param>
        /// <returns>
        /// The current <see cref="MessagingConfigurationBuilder"/>.
        /// </returns>
        /// <exception cref="ArgumentNullException">
        /// <paramref name="namingConvention"/> is <see langword="null"/>.
        /// </exception>
        public MessagingConfigurationBuilder WithQueueNamingConvention(IQueueNamingConvention namingConvention)
        {
            QueueNamingConvention = namingConvention ?? throw new ArgumentNullException(nameof(namingConvention));
            return this;
        }

        /// <summary>
        /// Specifies the <see cref="IQueueNamingConvention"/> to use.
        /// </summary>
        /// <typeparam name="T">The <see cref="IQueueNamingConvention"/> to use which will be resolved from the <see cref="IServiceResolver"/></typeparam>
        /// <returns>
        /// The current <see cref="MessagingConfigurationBuilder"/>.
        /// </returns>
        /// <exception cref="ArgumentNullException">
        /// <typeparamref name="T"/> is resolved to <see langword="null"/>.
        /// </exception>
        public MessagingConfigurationBuilder WithQueueNamingConvention<T>() where T : class, IQueueNamingConvention
        {
            return WithQueueNamingConvention(BusBuilder.ServiceResolver.ResolveService<T>());
        }

        /// <summary>
        /// Creates a new instance of <see cref="IMessagingConfig"/>.
        /// </summary>
        /// <returns>
        /// The created instance of <see cref="IMessagingConfig"/>.
        /// </returns>
        public IMessagingConfig Build()
        {
            var config = BusBuilder.ServiceResolver.ResolveService<IMessagingConfig>();

            if (Region != null)
            {
                config.Region = Region;
            }

            if (AdditionalSubscriberAccounts?.Count > 0)
            {
                config.AdditionalSubscriberAccounts = AdditionalSubscriberAccounts;
            }

            if (MessageResponseLogger != null)
            {
                config.MessageResponseLogger = MessageResponseLogger;
            }

            if (MessageSubjectProvider != null)
            {
                config.MessageSubjectProvider = MessageSubjectProvider;
            }

            if (TopicNamingConvention != null)
            {
                config.TopicNamingConvention = TopicNamingConvention;
            }

            if (QueueNamingConvention != null)
            {
                config.QueueNamingConvention = QueueNamingConvention;
            }

            if (PublishFailureBackoff.HasValue)
            {
                config.PublishFailureBackoff = PublishFailureBackoff.Value;
            }

            if (PublishFailureReAttempts.HasValue)
            {
                config.PublishFailureReAttempts = PublishFailureReAttempts.Value;
            }

            return config;
        }
    }
}
