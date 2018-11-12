using System;
using System.ComponentModel;
using System.Globalization;

namespace JustSaying.Extensions
{
    [EditorBrowsable(EditorBrowsableState.Never)]
    internal static class TimespanExtensions
    {
        // Convert the duration from TimeSpan
        // to an integer number of seconds, in a string
        // As AWS requires
        public static string AsSecondsString(this TimeSpan value)
        {
            return value.TotalSeconds.ToString("F0", CultureInfo.InvariantCulture);
        }
    }
}
