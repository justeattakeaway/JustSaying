using System;
using System.Linq;
using System.Reflection;
using System.Text.RegularExpressions;

namespace JustSaying.Extensions
{
    internal static class TypeExtensions
    {
        private const int MAX_TOPIC_NAME_LENGTH = 256;

        public static string ToTopicName(this Type type)
        {
            var name = type.GetTypeInfo().IsGenericType
                ? Regex.Replace(type.FullName, "\\W", "_").ToLower()
                : type.Name.ToLower();

            if (name.Length > MAX_TOPIC_NAME_LENGTH)
            {
                var suffix = name.GetInvariantHashCode().ToString();
                name = name.Substring(0, MAX_TOPIC_NAME_LENGTH - suffix.Length) + suffix;
            }

            return name;
        }

        private static int GetInvariantHashCode(this string value)
        {
            return value.Aggregate(5381, (current, character) => (current*397) ^ character);
        }
    }
}