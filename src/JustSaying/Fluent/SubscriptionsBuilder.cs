using System;
using System.Collections.Generic;
using JustSaying.Messaging.Channels.SubscriptionGroups;
using JustSaying.Models;

namespace JustSaying.Fluent
{
    /// <summary>
    /// A class representing a builder for subscriptions. This class cannot be inherited.
    /// </summary>
    public sealed class SubscriptionsBuilder
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="SubscriptionsBuilder"/> class.
        /// </summary>
        /// <param name="parent">The <see cref="MessagingBusBuilder"/> that owns this instance.</param>
        internal SubscriptionsBuilder(MessagingBusBuilder parent)
        {
            Parent = parent;
        }

        /// <summary>
        /// Gets the parent of this builder.
        /// </summary>
        internal MessagingBusBuilder Parent { get; }

        /// <summary>
        /// Gets the configured subscription builders.
        /// </summary>
        private IList<ISubscriptionBuilder<Message>> Subscriptions { get; } = new List<ISubscriptionBuilder<Message>>();

        private IDictionary<string, SubscriptionGroupSettingsBuilder> SubscriptionGroupSettings { get; } =
            new Dictionary<string, SubscriptionGroupSettingsBuilder>();

        /// <summary>
        /// Configures a queue subscription for the default queue.
        /// </summary>
        /// <typeparam name="T">The type of the message to subscribe to.</typeparam>
        /// <returns>
        /// The current <see cref="SubscriptionsBuilder"/>.
        /// </returns>
        /// <exception cref="ArgumentNullException">
        /// <paramref name="configure"/> is <see langword="null"/>.
        /// </exception>
        public SubscriptionsBuilder ForQueue<T>()
            where T : Message
        {
            return ForQueue<T>((p) => p.WithDefaultQueue());
        }

        /// <summary>
        /// Configures a queue subscription for the specified queue name.
        /// </summary>
        /// <typeparam name="T">The type of the message to subscribe to.</typeparam>
        /// <param name="name">The name of the queue to subscribe to.</param>
        /// <returns>
        /// The current <see cref="SubscriptionsBuilder"/>.
        /// </returns>
        /// <exception cref="ArgumentNullException">
        /// <paramref name="configure"/> is <see langword="null"/>.
        /// </exception>
        public SubscriptionsBuilder ForQueue<T>(string name)
            where T : Message
        {
            return ForQueue<T>((p) => p.WithName(name));
        }

        /// <summary>
        /// Configures a queue subscription.
        /// </summary>
        /// <typeparam name="T">The type of the message to subscribe to.</typeparam>
        /// <param name="configure">A delegate to a method to use to configure a queue subscription.</param>
        /// <returns>
        /// The current <see cref="SubscriptionsBuilder"/>.
        /// </returns>
        /// <exception cref="ArgumentNullException">
        /// <paramref name="configure"/> is <see langword="null"/>.
        /// </exception>
        public SubscriptionsBuilder ForQueue<T>(Action<QueueSubscriptionBuilder<T>> configure)
            where T : Message
        {
            if (configure == null)
            {
                throw new ArgumentNullException(nameof(configure));
            }

            var builder = new QueueSubscriptionBuilder<T>();

            configure(builder);

            Subscriptions.Add(builder);

            return this;
        }

        /// <summary>
        /// Configures a topic subscription for the default topic name.
        /// </summary>
        /// <typeparam name="T">The type of the message to subscribe to.</typeparam>
        /// <returns>
        /// The current <see cref="SubscriptionsBuilder"/>.
        /// </returns>
        public SubscriptionsBuilder ForTopic<T>()
            where T : Message
        {
            return ForTopic<T>((p) => p.IntoDefaultTopic());
        }

        /// <summary>
        /// Configures a topic subscription for the specified topic name.
        /// </summary>
        /// <typeparam name="T">The type of the message to subscribe to.</typeparam>
        /// <param name="name">The name of the topic to subscribe to.</param>
        /// <returns>
        /// The current <see cref="SubscriptionsBuilder"/>.
        /// </returns>
        /// <exception cref="ArgumentNullException">
        /// <paramref name="configure"/> is <see langword="null"/>.
        /// </exception>
        public SubscriptionsBuilder ForTopic<T>(string name)
            where T : Message
        {
            return ForTopic<T>((p) => p.WithName(name));
        }

        /// <summary>
        /// Configures a topic subscription.
        /// </summary>
        /// <typeparam name="T">The type of the message to subscribe to.</typeparam>
        /// <param name="configure">A delegate to a method to use to configure a topic subscription.</param>
        /// <returns>
        /// The current <see cref="SubscriptionsBuilder"/>.
        /// </returns>
        /// <exception cref="ArgumentNullException">
        /// <paramref name="configure"/> is <see langword="null"/>.
        /// </exception>
        public SubscriptionsBuilder ForTopic<T>(Action<TopicSubscriptionBuilder<T>> configure)
            where T : Message
        {
            if (configure == null)
            {
                throw new ArgumentNullException(nameof(configure));
            }

            var builder = new TopicSubscriptionBuilder<T>();

            configure(builder);

            Subscriptions.Add(builder);

            return this;
        }

        /// <summary>
        /// Configures the subscriptions for the <see cref="JustSayingFluently"/>.
        /// </summary>
        /// <param name="bus">The <see cref="JustSayingFluently"/> to configure subscriptions for.</param>
        /// <exception cref="InvalidOperationException">
        /// No instance of <see cref="IHandlerResolver"/> could be resolved.
        /// </exception>
        internal void Configure(JustSayingFluently bus)
        {
            var resolver = Parent.ServicesBuilder?.HandlerResolver?.Invoke() ??
                Parent.ServiceResolver.ResolveService<IHandlerResolver>();

            if (resolver == null)
            {
                throw new InvalidOperationException($"No {nameof(IHandlerResolver)} is registered.");
            }

            foreach (ISubscriptionBuilder<Message> builder in Subscriptions)
            {
                builder.Configure(bus, resolver);
            }
        }

        public SubscriptionsBuilder WithSubscriptionGroup(
            string groupName,
            Action<SubscriptionGroupSettingsBuilder> action)
        {
            if (string.IsNullOrEmpty(groupName)) throw new ArgumentNullException(nameof(groupName));
            if (action == null) throw new ArgumentNullException(nameof(action));

            if (SubscriptionGroupSettings.TryGetValue(groupName, out var settings))
            {
                action.Invoke(settings);
            }
            else
            {
                var newSettings = new SubscriptionGroupSettingsBuilder(groupName);
                action.Invoke(newSettings);
                SubscriptionGroupSettings.Add(groupName, newSettings);
            }

            return this;
        }
    }
}
