using System;
using Amazon;

namespace JustSaying.Fluent
{
    public sealed class TopicAddress
    {
        internal TopicAddress()
        { }

        private TopicAddress(bool isNone)
        {
            _isNone = isNone;
        }

        public string TopicArn { get; internal set; }

        private readonly bool _isNone = false;

        /// <summary>
        /// Use <see cref="None"/> to have JustSaying automatically create your topic.
        /// </summary>
        public static TopicAddress None { get; } = new (true);

        public static TopicAddress FromArn(string topicArn)
        {
            if (!Arn.IsArn(topicArn) || !Arn.TryParse(topicArn, out var arn)) throw new ArgumentException("Must be a valid ARN.", nameof(topicArn));
            if (arn.Service != "sns") throw new ArgumentException("Must be an ARN for an SNS topic.");
            return new TopicAddress { TopicArn = topicArn };
        }
    }
}
