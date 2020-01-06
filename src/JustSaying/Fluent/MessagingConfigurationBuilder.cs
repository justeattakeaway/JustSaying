using System;
using System.Collections.Generic;
using System.Linq;
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

        /// <inheritdoc />
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
        /// Gets or sets the optional value to use for <see cref="IMessagingConfig.GetActiveRegion"/>
        /// </summary>
        private Func<string> GetActiveRegion { get; set; }

        /// <summary>
        /// Gets or sets the optional value to use for <see cref="IMessagingConfig.Regions"/>
        /// </summary>
        private List<string> Regions { get; set; }

        /// <summary>
        /// Gets or sets the optional value to use for <see cref="IMessagingConfig.MessageSubjectProvider"/>
        /// </summary>
        private IMessageSubjectProvider MessageSubjectProvider { get; set; }

        /// <summary>
        /// Gets or sets the optional value to use for <see cref="IMessagingConfig.DefaultTopicNamingConvention"/>
        /// </summary>
        private IDefaultTopicNamingConvention DefaultTopicNamingConvention { get; set; }

        /// <summary>
        /// Gets or sets the optional value to use for <see cref="IMessagingConfig.DefaultQueueNamingConvention"/>
        /// </summary>
        private IDefaultQueueNamingConvention DefaultQueueNamingConvention { get; set; }

        /// <summary>
        /// Specifies the active AWS region to use.
        /// </summary>
        /// <param name="region">The active AWS region to use.</param>
        /// <returns>
        /// The current <see cref="MessagingConfigurationBuilder"/>.
        /// </returns>
        /// <exception cref="ArgumentNullException">
        /// <paramref name="region"/> is <see cref="null"/>.
        /// </exception>
        public MessagingConfigurationBuilder WithActiveRegion(RegionEndpoint region)
        {
            if (region == null)
            {
                throw new ArgumentNullException(nameof(region));
            }

            return WithActiveRegion(region.SystemName);
        }

        /// <summary>
        /// Specifies the active AWS region to use.
        /// </summary>
        /// <param name="region">The active AWS region to use.</param>
        /// <returns>
        /// The current <see cref="MessagingConfigurationBuilder"/>.
        /// </returns>
        /// <exception cref="ArgumentNullException">
        /// <paramref name="region"/> is <see cref="null"/>.
        /// </exception>
        public MessagingConfigurationBuilder WithActiveRegion(string region)
        {
            if (region == null)
            {
                throw new ArgumentNullException(nameof(region));
            }

            return WithActiveRegion(() => region);
        }

        /// <summary>
        /// Specifies a delegate which evaluates the current active AWS region to use.
        /// </summary>
        /// <param name="evaluator">A delegate to a method with evaluates the active AWS region to use.</param>
        /// <returns>
        /// The current <see cref="MessagingConfigurationBuilder"/>.
        /// </returns>
        /// <exception cref="ArgumentNullException">
        /// <paramref name="evaluator"/> is <see cref="null"/>.
        /// </exception>
        public MessagingConfigurationBuilder WithActiveRegion(Func<string> evaluator)
        {
            GetActiveRegion = evaluator ?? throw new ArgumentNullException(nameof(evaluator));
            return this;
        }

        /// <summary>
        /// Specifies additional subscriber account(s) to use.
        /// </summary>
        /// <param name="regions">The AWS account Id(s) to additionally subscribe to.</param>
        /// <returns>
        /// The current <see cref="MessagingConfigurationBuilder"/>.
        /// </returns>
        /// <exception cref="ArgumentNullException">
        /// <paramref name="regions"/> is <see cref="null"/>.
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
        /// <paramref name="accountIds"/> is <see cref="null"/>.
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
        /// <paramref name="accountId"/> is <see cref="null"/>.
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
        /// <paramref name="logger"/> is <see cref="null"/>.
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
        /// <paramref name="subjectProvider"/> is <see cref="null"/>.
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
        /// Specifies the AWS region(s) to use.
        /// </summary>
        /// <param name="regions">The AWS region(s) to use.</param>
        /// <returns>
        /// The current <see cref="MessagingConfigurationBuilder"/>.
        /// </returns>
        /// <exception cref="ArgumentNullException">
        /// <paramref name="regions"/> is <see cref="null"/>.
        /// </exception>
        public MessagingConfigurationBuilder WithRegions(params string[] regions)
            => WithRegions(regions as IEnumerable<string>);

        /// <summary>
        /// Specifies the AWS region(s) to use.
        /// </summary>
        /// <param name="regions">The AWS region(s) to use.</param>
        /// <returns>
        /// The current <see cref="MessagingConfigurationBuilder"/>.
        /// </returns>
        /// <exception cref="ArgumentNullException">
        /// <paramref name="regions"/> is <see cref="null"/>.
        /// </exception>
        public MessagingConfigurationBuilder WithRegions(IEnumerable<string> regions)
        {
            if (regions == null)
            {
                throw new ArgumentNullException(nameof(regions));
            }

            Regions = new List<string>(regions);
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
        /// <paramref name="region"/> is <see cref="null"/>.
        /// </exception>
        public MessagingConfigurationBuilder WithRegion(string region)
        {
            if (region == null)
            {
                throw new ArgumentNullException(nameof(region));
            }

            if (Regions == null)
            {
                Regions = new List<string>();
            }

            if (!Regions.Contains(region))
            {
                Regions.Add(region);
            }

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
        /// <paramref name="region"/> is <see cref="null"/>.
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
        /// Specifies the AWS region(s) to use.
        /// </summary>
        /// <param name="regions">The AWS region(s) to use.</param>
        /// <returns>
        /// The current <see cref="MessagingConfigurationBuilder"/>.
        /// </returns>
        /// <exception cref="ArgumentNullException">
        /// <paramref name="regions"/> is <see cref="null"/>.
        /// </exception>
        public MessagingConfigurationBuilder WithRegions(params RegionEndpoint[] regions)
            => WithRegions(regions as IEnumerable<RegionEndpoint>);

        /// <summary>
        /// Specifies the AWS region(s) to use.
        /// </summary>
        /// <param name="regions">The AWS region(s) to use.</param>
        /// <returns>
        /// The current <see cref="MessagingConfigurationBuilder"/>.
        /// </returns>
        /// <exception cref="ArgumentNullException">
        /// <paramref name="regions"/> is <see cref="null"/>.
        /// </exception>
        public MessagingConfigurationBuilder WithRegions(IEnumerable<RegionEndpoint> regions)
        {
            if (regions == null)
            {
                throw new ArgumentNullException(nameof(regions));
            }

            Regions = new List<string>(regions.Select((p) => p.SystemName).Distinct(StringComparer.Ordinal));
            return this;
        }

        /// <summary>
        /// Specifies the <see cref="IDefaultTopicNamingConvention"/> to use.
        /// </summary>
        /// <param name="namingConvention">The <see cref="IDefaultTopicNamingConvention"/> to use.</param>
        /// <returns>
        /// The current <see cref="MessagingConfigurationBuilder"/>.
        /// </returns>
        /// <exception cref="ArgumentNullException">
        /// <paramref name="namingConvention"/> is <see cref="null"/>.
        /// </exception>
        public MessagingConfigurationBuilder WithDefaultTopicNamingConvention(IDefaultTopicNamingConvention namingConvention)
        {
            DefaultTopicNamingConvention = namingConvention ?? throw new ArgumentNullException(nameof(namingConvention));
            return this;
        }

        /// <summary>
        /// Specifies the <see cref="IDefaultTopicNamingConvention"/> to use.
        /// </summary>
        /// <typeparam name="T">The <see cref="IDefaultTopicNamingConvention"> to use which will be resolved from the <see cref="IServiceResolver"/></typeparam>
        /// <returns>
        /// The current <see cref="MessagingConfigurationBuilder"/>.
        /// </returns>
        /// <exception cref="ArgumentNullException">
        /// <paramref name="T"/> is resolved to <see cref="null"/>.
        /// </exception>
        public MessagingConfigurationBuilder WithDefaultTopicNamingConvention<T>() where T : class, IDefaultTopicNamingConvention
        {
            return WithDefaultTopicNamingConvention(BusBuilder.ServiceResolver.ResolveService<T>());
        }

        /// <summary>
        /// Specifies the <see cref="IDefaultQueueNamingConvention"/> to use.
        /// </summary>
        /// <param name="namingConvention">The <see cref="IDefaultQueueNamingConvention"/> to use.</param>
        /// <returns>
        /// The current <see cref="MessagingConfigurationBuilder"/>.
        /// </returns>
        /// <exception cref="ArgumentNullException">
        /// <paramref name="namingConvention"/> is <see cref="null"/>.
        /// </exception>
        public MessagingConfigurationBuilder WithDefaultQueueNamingConvention(IDefaultQueueNamingConvention namingConvention)
        {
            DefaultQueueNamingConvention = namingConvention ?? throw new ArgumentNullException(nameof(namingConvention));
            return this;
        }

        /// <summary>
        /// Specifies the <see cref="IDefaultQueueNamingConvention"/> to use.
        /// </summary>
        /// <typeparam name="T">The <see cref="IDefaultQueueNamingConvention"> to use which will be resolved from the <see cref="IServiceResolver"/></typeparam>
        /// <returns>
        /// The current <see cref="MessagingConfigurationBuilder"/>.
        /// </returns>
        /// <exception cref="ArgumentNullException">
        /// <paramref name="T"/> is resolved to <see cref="null"/>.
        /// </exception>
        public MessagingConfigurationBuilder WithDefaultQueueNamingConvention<T>() where T : class, IDefaultQueueNamingConvention
        {
            return WithDefaultQueueNamingConvention(BusBuilder.ServiceResolver.ResolveService<T>());
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

            if (Regions?.Count > 0)
            {
                foreach (string region in Regions)
                {
                    if (!config.Regions.Contains(region))
                    {
                        config.Regions.Add(region);
                    }
                }
            }

            if (AdditionalSubscriberAccounts?.Count > 0)
            {
                config.AdditionalSubscriberAccounts = AdditionalSubscriberAccounts;
            }

            if (GetActiveRegion != null)
            {
                config.GetActiveRegion = GetActiveRegion;
            }

            if (MessageResponseLogger != null)
            {
                config.MessageResponseLogger = MessageResponseLogger;
            }

            if (MessageSubjectProvider != null)
            {
                config.MessageSubjectProvider = MessageSubjectProvider;
            }

            if (DefaultTopicNamingConvention != null)
            {
                config.DefaultTopicNamingConvention = DefaultTopicNamingConvention;
            }

            if (DefaultQueueNamingConvention != null)
            {
                config.DefaultQueueNamingConvention = DefaultQueueNamingConvention;
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
