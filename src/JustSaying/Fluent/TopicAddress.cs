using System;
using Amazon;

namespace JustSaying.Fluent
{
    public sealed class TopicAddress
    {
        internal TopicAddress()
        { }

        public string TopicArn { get; internal set; }

        public static TopicAddress FromArn(string topicArn)
        {
            if (!Arn.IsArn(topicArn) || !Arn.TryParse(topicArn, out _)) throw new ArgumentException("Must be a valid ARN.", nameof(topicArn));
            return new TopicAddress { TopicArn = topicArn };
        }
    }
}
