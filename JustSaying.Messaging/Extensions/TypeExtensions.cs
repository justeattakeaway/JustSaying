using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.RegularExpressions;

namespace JustSaying.Messaging.Extensions
{
    public static class TypeExtensions
    {
        public static string ToKey(this Type type)
        {
            return string.Join("_", type.Flatten().Select(t => Regex.Replace(t.Name + "_" + t.Namespace, "\\W", "_"))).TruncateTo(100);
        }

        private static IEnumerable<Type> Flatten(this Type type)
        {
            yield return type;
            foreach (var inner in type.GetGenericArguments().SelectMany(t => t.Flatten()))
            {
                yield return inner;
            }
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
            return value.Aggregate(5381, (current, character) => (current * 397) ^ character);
        }

    }
}