using System.Globalization;
using System.Text.RegularExpressions;

namespace JustSaying.CloudEvents;

/// <summary>
/// CloudEvents 1.0 attribute rules shared by the <see cref="CloudEvent{T}"/> envelope and its serializer.
/// </summary>
internal static class CloudEventAttributes
{
    public const string SpecVersion = "1.0";

    // The CloudEvents 1.0 context attribute names, plus the JSON format's data members; everything else
    // at the top level of a structured-mode event is an extension attribute.
    public static readonly HashSet<string> Reserved = new(StringComparer.Ordinal)
    {
        "specversion", "id", "source", "type", "time", "datacontenttype", "dataschema", "subject", "data", "data_base64",
    };

    // RFC 3339 `date-time`: a full date and time with an explicit offset. [0-9] rather than \d, which
    // also matches non-ASCII digits.
    private static readonly Regex Rfc3339 = new(
        "^([0-9]{4})-([0-9]{2})-([0-9]{2})[Tt]([0-9]{2}):([0-9]{2}):([0-9]{2})(?:\\.([0-9]+))?(?:([Zz])|([+-])([0-9]{2}):([0-9]{2}))$",
        RegexOptions.CultureInvariant);

    public static void ValidateExtensionNames(IReadOnlyDictionary<string, string> extensions, string parameterName)
    {
        foreach (var name in extensions.Keys)
        {
            if (!IsValidExtensionName(name))
            {
                throw new ArgumentException(
                    $"'{name}' is not a valid CloudEvents extension attribute name; names must consist of lowercase letters (a-z) and digits (0-9) only.",
                    parameterName);
            }

            if (Reserved.Contains(name))
            {
                throw new ArgumentException(
                    $"'{name}' is a CloudEvents context attribute, so it can't be used as an extension attribute name; set it through the CloudEvent<T> constructor instead.",
                    parameterName);
            }
        }
    }

    /// <summary>
    /// Parses an RFC 3339 timestamp. A value without an offset is rejected rather than being read in the
    /// machine's local time zone. A leap second (<c>:60</c>), which <see cref="DateTimeOffset"/> can't
    /// represent, is read as the first instant of the next minute.
    /// </summary>
    public static bool TryParseTime(string value, out DateTimeOffset time)
    {
        time = default;

        var match = Rfc3339.Match(value ?? string.Empty);
        if (!match.Success)
        {
            return false;
        }

        int Part(int group) => int.Parse(match.Groups[group].Value, NumberStyles.None, CultureInfo.InvariantCulture);

        var second = Part(6);
        var leapSecond = second == 60;

        var offset = TimeSpan.Zero;
        if (!match.Groups[8].Success)
        {
            if (Part(11) > 59)
            {
                return false;
            }

            offset = new TimeSpan(Part(10), Part(11), 0);
            if (match.Groups[9].Value == "-")
            {
                offset = offset.Negate();
            }
        }

        // DateTimeOffset holds 100ns ticks, so finer digits (RFC 3339 allows any number) are truncated.
        var fraction = match.Groups[7].Success ? match.Groups[7].Value : string.Empty;
        var ticks = fraction.Length == 0
            ? 0
            : long.Parse(fraction.Length > 7 ? fraction.Substring(0, 7) : fraction.PadRight(7, '0'), NumberStyles.None, CultureInfo.InvariantCulture);

        try
        {
            time = new DateTimeOffset(Part(1), Part(2), Part(3), Part(4), Part(5), leapSecond ? 59 : second, offset)
                .AddTicks(ticks)
                .AddSeconds(leapSecond ? 1 : 0);
            return true;
        }
        catch (ArgumentOutOfRangeException)
        {
            // An impossible date or time (month 13, hour 24, an offset beyond ±14:00).
            return false;
        }
    }

    /// <summary>
    /// Determines whether a <c>datacontenttype</c> describes JSON data (<c>application/json</c>,
    /// <c>text/json</c> or any <c>+json</c> suffix type), which is the only data the serializers read.
    /// An absent content type implies JSON in the CloudEvents JSON format.
    /// </summary>
    public static bool IsJsonContentType(string contentType)
    {
        if (string.IsNullOrEmpty(contentType))
        {
            return true;
        }

        var parametersStart = contentType.IndexOf(';');
        var mediaType = (parametersStart >= 0 ? contentType.Substring(0, parametersStart) : contentType).Trim();

        return mediaType.Equals("application/json", StringComparison.OrdinalIgnoreCase)
               || mediaType.Equals("text/json", StringComparison.OrdinalIgnoreCase)
               || mediaType.EndsWith("+json", StringComparison.OrdinalIgnoreCase);
    }

    private static bool IsValidExtensionName(string name)
    {
        if (string.IsNullOrEmpty(name))
        {
            return false;
        }

        foreach (var c in name)
        {
            if (c is not (>= 'a' and <= 'z') and not (>= '0' and <= '9'))
            {
                return false;
            }
        }

        return true;
    }
}
