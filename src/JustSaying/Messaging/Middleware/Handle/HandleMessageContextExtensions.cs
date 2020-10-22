
using System;
using JustSaying.Models;

namespace JustSaying.Messaging.Middleware.Handle
{
    public static class HandleMessageContextExtensions
    {
        public static TMessage MessageAs<TMessage>(this HandleMessageContext context) where TMessage : Message
        {
            if(context == null) throw new ArgumentNullException(nameof(context));

            return context.Message as TMessage;
        }
    }
}
