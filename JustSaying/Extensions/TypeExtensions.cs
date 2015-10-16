using System;
using System.Linq;
using JustSaying.Messaging.Extensions;

namespace JustSaying.Extensions
{
    public static class TypeExtensions
    {
        private const int MAX_TOPIC_NAME_LENGTH = 256;

        public static string ToTopicName(this Type type)
        {
            var name = type.ToKey().TruncateTo(MAX_TOPIC_NAME_LENGTH);
            return name;
        }

        private static string TruncateTo(this string name, int maxLength)
        {
            if (name.Length <= maxLength) return name;

            var suffix = name.GetInvariantHashCode().ToString();
            name = name.Substring(0, maxLength - suffix.Length) + suffix;

            return name;
        }


        private static int GetInvariantHashCode(this string value)
        {
            return value.Aggregate(5381, (current, character) => (current*397) ^ character);
        }
    }
}