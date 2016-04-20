using System;
using JustSaying.Messaging.MessageHandling;
using JustSaying.Models;

namespace JustSaying.AwsTools.MessageHandling
{
    internal class GuaranteedOnceDelivery<T> where T : Message
    {
        private const int DEFAULT_TEMPORARY_LOCK_SECONDS = 30;
        private readonly IHandlerAsync<T> _handler;

        public GuaranteedOnceDelivery(IHandlerAsync<T> handler)
        {
            _handler = handler;
        }

        public bool Enabled
        {
            get { return Attribute.IsDefined(_handler.GetType(), typeof(ExactlyOnceAttribute)); }
        }

        public int TimeOut
        {
            get
            {
                var attributes = _handler.GetType().GetCustomAttributes(true);
                foreach (var attribute in attributes)
                {
                    var exactlyOnce = attribute as ExactlyOnceAttribute;
                    if (exactlyOnce != null)
                    {
                        return exactlyOnce.TimeOut;
                    }
                }
                return DEFAULT_TEMPORARY_LOCK_SECONDS;
            }
        }
    }
}