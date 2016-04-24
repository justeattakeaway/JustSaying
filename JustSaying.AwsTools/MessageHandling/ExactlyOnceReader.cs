using System;
using System.Linq;
using JustSaying.Messaging.MessageHandling;

namespace JustSaying.AwsTools.MessageHandling
{
    internal class ExactlyOnceReader
    {
        private const int DefaultTemporaryLockSeconds = 30;
        private readonly Type _type;

        public ExactlyOnceReader(Type type)
        {
            _type = type;
        }

        public bool Enabled
        {
            get { return Attribute.IsDefined(_type, typeof(ExactlyOnceAttribute)); }
        }

        public int GetTimeOut()
        {
            var attributes = _type.GetCustomAttributes(true);
            var targetAttribute = attributes.FirstOrDefault(a => a is ExactlyOnceAttribute);

            if (targetAttribute != null)
            {
                var exactlyOnce = (ExactlyOnceAttribute)targetAttribute;
                return exactlyOnce.TimeOut;
            }

            return DefaultTemporaryLockSeconds;
        }
    }
}