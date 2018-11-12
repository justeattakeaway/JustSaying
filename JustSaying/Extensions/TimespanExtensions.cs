using System;
using System.Globalization;

namespace JustSaying.Extensions
{
    public static class TimespanExtensions
    {
        /// <summary>
        /// Convert the duration to an integer number of seconds, in a string
        /// As AWS requires
        /// </summary>
        /// <param name="value"></param>
        /// <returns></returns>
        public static string AsSecondsString(this TimeSpan value)
        {
            return value.TotalSeconds.ToString("F0", CultureInfo.InvariantCulture);
        }
    }
}
