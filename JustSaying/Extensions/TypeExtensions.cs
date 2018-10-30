using System;
using System.Reflection;
using System.Text.RegularExpressions;

namespace JustSaying.Extensions
{
    public static class TypeExtensions
    {
        private const int MAX_TOPIC_NAME_LENGTH = 256;

        public static string ToTopicName(this Type type)
        {
            var name = type.GetTypeInfo().IsGenericType
                ? Regex.Replace(type.FullName, "\\W", "_").ToLowerInvariant()
                : type.Name.ToLowerInvariant();

            return name.TruncateTo(MAX_TOPIC_NAME_LENGTH);
        }
    }
}
