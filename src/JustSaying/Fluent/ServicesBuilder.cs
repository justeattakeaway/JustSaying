using System;
using JustSaying.Messaging.Channels;
using JustSaying.Messaging.MessageHandling;
using JustSaying.Messaging.MessageSerialization;
using JustSaying.Messaging.Monitoring;
using Microsoft.Extensions.Logging;

namespace JustSaying.Fluent
{
    /// <summary>
    /// Defines a builder for services used by JustSaying. This class cannot be inherited.
    /// </summary>
    public sealed class ServicesBuilder
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="ServicesBuilder"/> class.
        /// </summary>
        /// <param name="busBuilder">The <see cref="MessagingBusBuilder"/> that owns this instance.</param>
        internal ServicesBuilder(MessagingBusBuilder busBuilder)
        {
            BusBuilder = busBuilder;
        }

        /// <inheritdoc />
        internal MessagingBusBuilder BusBuilder { get; }

        /// <summary>
        /// Gets or sets a delegate to a method to create the <see cref="IHandlerResolver"/> to use.
        /// </summary>
        internal Func<IHandlerResolver> HandlerResolver { get; private set; }

        /// <summary>
        /// Gets or sets a delegate to a method to create the <see cref="ILoggerFactory"/> to use.
        /// </summary>
        internal Func<ILoggerFactory> LoggerFactory { get; private set; }

        /// <summary>
        /// Gets or sets a delegate to a method to create the <see cref="IMessageMonitor"/> to use.
        /// </summary>
        internal Func<IMessageMonitor> MessageMonitoring { get; private set; }

        /// <summary>
        /// Gets or sets a delegate to a method to create the <see cref="IMessageLockAsync"/> to use.
        /// </summary>
        internal Func<IMessageLockAsync> MessageLock { get; private set; }

        /// <summary>
        /// Gets or sets a delegate to a method to create the <see cref="IMessageSerializationRegister"/> to use.
        /// </summary>
        internal Func<IMessageSerializationRegister> SerializationRegister { get; private set; }

        /// <summary>
        /// Gets or sets a delegate to a method to create the <see cref="MessageContextAccessor"/> to use.
        /// </summary>
        internal Func<IMessageContextAccessor> MessageContextAccessor { get; private set; }

        /// <summary>
        /// Specifies the <see cref="IHandlerResolver"/> to use.
        /// </summary>
        /// <param name="handlerResolver">The <see cref="IHandlerResolver"/> to use.</param>
        /// <returns>
        /// The current <see cref="ServicesBuilder"/>.
        /// </returns>
        /// <exception cref="ArgumentNullException">
        /// <paramref name="handlerResolver"/> is <see langword="null"/>.
        /// </exception>
        public ServicesBuilder WithHandlerResolver(IHandlerResolver handlerResolver)
        {
            if (handlerResolver == null)
            {
                throw new ArgumentNullException(nameof(handlerResolver));
            }

            return WithHandlerResolver(() => handlerResolver);
        }

        /// <summary>
        /// Specifies the <see cref="IHandlerResolver"/> to use.
        /// </summary>
        /// <param name="handlerResolver">A delegate to a method to get the <see cref="IHandlerResolver"/> to use.</param>
        /// <returns>
        /// The current <see cref="ServicesBuilder"/>.
        /// </returns>
        /// <exception cref="ArgumentNullException">
        /// <paramref name="handlerResolver"/> is <see langword="null"/>.
        /// </exception>
        public ServicesBuilder WithHandlerResolver(Func<IHandlerResolver> handlerResolver)
        {
            HandlerResolver = handlerResolver ?? throw new ArgumentNullException(nameof(handlerResolver));
            return this;
        }

        /// <summary>
        /// Specifies the <see cref="ILoggerFactory"/> to use.
        /// </summary>
        /// <param name="loggerFactory">The <see cref="ILoggerFactory"/> to use.</param>
        /// <returns>
        /// The current <see cref="ServicesBuilder"/>.
        /// </returns>
        /// <exception cref="ArgumentNullException">
        /// <paramref name="loggerFactory"/> is <see langword="null"/>.
        /// </exception>
        public ServicesBuilder WithLoggerFactory(ILoggerFactory loggerFactory)
        {
            if (loggerFactory == null)
            {
                throw new ArgumentNullException(nameof(loggerFactory));
            }

            return WithLoggerFactory(() => loggerFactory);
        }

        /// <summary>
        /// Specifies the <see cref="ILoggerFactory"/> to use.
        /// </summary>
        /// <param name="loggerFactory">A delegate to a method to get the <see cref="ILoggerFactory"/> to use.</param>
        /// <returns>
        /// The current <see cref="ServicesBuilder"/>.
        /// </returns>
        /// <exception cref="ArgumentNullException">
        /// <paramref name="loggerFactory"/> is <see langword="null"/>.
        /// </exception>
        public ServicesBuilder WithLoggerFactory(Func<ILoggerFactory> loggerFactory)
        {
            LoggerFactory = loggerFactory ?? throw new ArgumentNullException(nameof(loggerFactory));
            return this;
        }

        /// <summary>
        /// Specifies the <see cref="IMessageLockAsync"/> to use.
        /// </summary>
        /// <param name="messageLock">A delegate to a method to get the <see cref="IMessageLockAsync"/> to use.</param>
        /// <returns>
        /// The current <see cref="ServicesBuilder"/>.
        /// </returns>
        /// <exception cref="ArgumentNullException">
        /// <paramref name="messageLock"/> is <see langword="null"/>.
        /// </exception>
        public ServicesBuilder WithMessageLock(Func<IMessageLockAsync> messageLock)
        {
            MessageLock = messageLock ?? throw new ArgumentNullException(nameof(messageLock));
            return this;
        }

        /// <summary>
        /// Specifies the <see cref="IMessageMonitor"/> to use.
        /// </summary>
        /// <param name="monitoring">A delegate to a method to get the <see cref="IMessageMonitor"/> to use.</param>
        /// <returns>
        /// The current <see cref="ServicesBuilder"/>.
        /// </returns>
        /// <exception cref="ArgumentNullException">
        /// <paramref name="monitoring"/> is <see langword="null"/>.
        /// </exception>
        public ServicesBuilder WithMessageMonitoring(Func<IMessageMonitor> monitoring)
        {
            MessageMonitoring = monitoring ?? throw new ArgumentNullException(nameof(monitoring));
            return this;
        }

        /// <summary>
        /// Specifies the <see cref="IMessageContextAccessor"/> to use.
        /// </summary>
        /// <param name="contextAccessor">A delegate to a method to get the <see cref="IMessageContextAccessor"/> to use.</param>
        /// <returns>
        /// The current <see cref="ServicesBuilder"/>.
        /// </returns>
        /// <exception cref="ArgumentNullException">
        /// <paramref name="contextAccessor"/> is <see langword="null"/>.
        /// </exception>
        public ServicesBuilder WithMessageContextAccessor(Func<IMessageContextAccessor> contextAccessor)
        {
            MessageContextAccessor = contextAccessor ?? throw new ArgumentNullException(nameof(contextAccessor));
            return this;
        }
    }
}
