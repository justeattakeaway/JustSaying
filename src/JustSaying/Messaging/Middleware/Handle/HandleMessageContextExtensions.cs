using System;
using JustSaying.Models;

// ReSharper disable once CheckNamespace
namespace JustSaying.Messaging.Middleware
{
    public static class HandleMessageContextExtensions
    {
        /// <summary>
        /// A convenience method to get a message as <see cref="TMessage"/> from the context.
        /// </summary>
        /// <param name="context">The context to get the message from.</param>
        /// <typeparam name="TMessage">The type of the message to try and get from the context.</typeparam>
        /// <returns>An instance of <see cref="TMessage"/> or null if the message was not of type <see cref="TMessage"/></returns>
        /// <exception cref="ArgumentNullException">The <see cref="context"/> object is null.</exception>
        public static TMessage MessageAs<TMessage>(this HandleMessageContext context) where TMessage : Message
        {
            if (context == null) throw new ArgumentNullException(nameof(context));

            return context.Message as TMessage;
        }
    }
}
