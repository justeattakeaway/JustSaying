using System.Globalization;

namespace JustSaying.Extensions;

internal static class StringExtensions
{
    public static string TruncateTo(this string s, int maxLength)
    {
        if (s.Length > maxLength)
        {
            var suffix = s.GetInvariantHashCode().ToString(CultureInfo.InvariantCulture);
            s = s.Substring(0, maxLength - suffix.Length) + suffix;
        }

        return s;
    }

    public static TimeSpan FromSecondsString(this string seconds)
    {
        return TimeSpan.FromSeconds(int.Parse(seconds, NumberStyles.None));
    }

    private static int GetInvariantHashCode(this string value)
    {
        return value.Aggregate(5381, (current, character) => (current * 397) ^ character);
    }
}
