using System;
using System.Collections.Generic;
using JustSaying.Models;

namespace JustSaying.Fluent
{
    /// <summary>
    /// A class representing a builder for subscriptions. This class cannot be inherited.
    /// </summary>
    public sealed class SubscriptionBuilder
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="SubscriptionBuilder"/> class.
        /// </summary>
        /// <param name="parent">The <see cref="MessagingBusBuilder"/> that owns this instance.</param>
        internal SubscriptionBuilder(MessagingBusBuilder parent)
        {
            Parent = parent;
        }

        /// <summary>
        /// Gets the parent of this builder.
        /// </summary>
        public MessagingBusBuilder Parent { get; }

        /// <summary>
        /// Gets the configured handler registrations.
        /// </summary>
        private IList<Action<JustSayingFluently, IHandlerResolver>> Registrations = new List<Action<JustSayingFluently, IHandlerResolver>>();

        /// <summary>
        /// Registers a message handler.
        /// </summary>
        /// <typeparam name="T">The message type to register a handler for.</typeparam>
        /// <returns>
        /// The current <see cref="SubscriptionBuilder"/>.
        /// </returns>
        public SubscriptionBuilder WithHandler<T>()
            where T : Message
        {
            Registrations.Add((p, resolver) => p.WithMessageHandler<T>(resolver));
            return this;
        }

        /// <summary>
        /// Configures the subscriptions for the <see cref="JustSayingFluently"/>.
        /// </summary>
        /// <param name="bus">The <see cref="JustSayingFluently"/> to configure subscriptions for.</param>
        internal void Configure(JustSayingFluently bus)
        {
            IHandlerResolver resolver = Parent.ServiceResolver.ResolveService<IHandlerResolver>();

            foreach (Action<JustSayingFluently, IHandlerResolver> registration in Registrations)
            {
                registration(bus, resolver);
            }
        }
    }
}
