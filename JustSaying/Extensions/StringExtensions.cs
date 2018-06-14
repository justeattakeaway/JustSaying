using System.Linq;

namespace JustSaying.Extensions
{
    internal static class StringExtensions
    {
        public static string TruncateTo(this string s, int maxLength)
        {
            if (s.Length > maxLength)
            {
                var suffix = s.GetInvariantHashCode().ToString();
                s = s.Substring(0, maxLength - suffix.Length) + suffix;
            }

            return s;
        }

        private static int GetInvariantHashCode(this string value)
        {
            return value.Aggregate(5381, (current, character) => (current * 397) ^ character);
        }
    }
}
