using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.RegularExpressions;

namespace JustSaying.Extensions
{
    public static class TypeExtensions
    {
        private const int MAX_TOPIC_NAME_LENGTH = 256;

        public static string ToTopicName(this Type type)
        {
            var name = type.GetTopicName().TruncateTo(MAX_TOPIC_NAME_LENGTH);
            return name;
        }

        private static int GetInvariantHashCode(this string value)
        {
            return value.Aggregate(5381, (current, character) => (current * 397) ^ character);
        }

        private static string GetTopicName(this Type type)
        {
            var list = new List<string>();
            RecursiveGetTopicName(type, list);
            return string.Join("-", list);
        }

        private static string TruncateTo(this string name, int maxLength)
        {
            if (name.Length <= maxLength) return name;

            var suffix = name.GetInvariantHashCode().ToString();
            name = name.Substring(0, maxLength - suffix.Length) + suffix;

            return name;
        }

        private static void RecursiveGetTopicName(Type type, ICollection<string> types)
        {
            types.Add(Regex.Replace(type.Name, "\\W", "_").ToLower());
            foreach (var innerType in type.GetGenericArguments())
            {
                RecursiveGetTopicName(innerType, types);
            }
        }
    }
}