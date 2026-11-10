namespace JustSaying.CloudEvents;

internal static class JsonMediaType
{
    /// <summary>
    /// Determines whether a media type is JSON: <c>*/json</c> (such as <c>application/json</c>) or a
    /// structured <c>+json</c> suffix (such as <c>application/cloudevents+json</c>), ignoring any
    /// parameters such as <c>charset</c>. The JSON event format writes <c>data</c> as JSON only for these.
    /// </summary>
    public static bool IsJson(string mediaType)
    {
        if (string.IsNullOrWhiteSpace(mediaType))
        {
            return false;
        }

        var parametersStart = mediaType.IndexOf(';');
        var essence = (parametersStart < 0 ? mediaType : mediaType.Substring(0, parametersStart)).Trim();

        var slash = essence.IndexOf('/');
        if (slash <= 0 || slash == essence.Length - 1)
        {
            return false;
        }

        var subtype = essence.Substring(slash + 1);
        return subtype.Equals("json", StringComparison.OrdinalIgnoreCase)
            || subtype.EndsWith("+json", StringComparison.OrdinalIgnoreCase);
    }
}
