using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Text.RegularExpressions;

namespace JustSaying.Extensions
{
    public static class TypeExtensions
    {
        private const int MaxTopicNameLength = 256;
        private const int MaxQueueNameLength = 80;

        private static readonly Dictionary<Type, string> TypeToFriendlyName = new Dictionary<Type, string>
        {
            {typeof(string), "string"},
            {typeof(object), "object"},
            {typeof(bool), "bool"},
            {typeof(byte), "byte"},
            {typeof(char), "char"},
            {typeof(decimal), "decimal"},
            {typeof(double), "double"},
            {typeof(short), "short"},
            {typeof(int), "int"},
            {typeof(long), "long"},
            {typeof(sbyte), "sbyte"},
            {typeof(float), "float"},
            {typeof(ushort), "ushort"},
            {typeof(uint), "uint"},
            {typeof(ulong), "ulong"},
            {typeof(void), "void"}
        };

        public static string ToDefaultTopicName(this Type type) => CreateResourceName(type, MaxTopicNameLength);

        public static string ToDefaultQueueName(this Type type) => CreateResourceName(type, MaxQueueNameLength);

        private static string CreateResourceName(Type type, int maximumLength)
        {
            var name = Regex.Replace(type.ToTypeFriendlyName(), "[^a-zA-Z0-9_-]", string.Empty);

            return name.Length <= maximumLength ? name.ToLowerInvariant() : name.Substring(0, maximumLength);
        }

        private static string ToTypeFriendlyName(this Type type)
        {
            if (TypeToFriendlyName.TryGetValue(type, out string friendlyName))
            {
                return friendlyName;
            }

            friendlyName = type.Name;

            if (type.GetTypeInfo().IsGenericType)
            {
                var backtick = friendlyName.IndexOf('`');

                if (backtick > 0)
                {
                    friendlyName = friendlyName.Remove(backtick);
                }

                friendlyName += string.Join("_", type.GenericTypeArguments.Select(ToTypeFriendlyName));
            }

            if (type.IsArray)
            {
                return type.GetElementType().ToTypeFriendlyName() + "_";
            }

            return friendlyName;
        }
    }
}
