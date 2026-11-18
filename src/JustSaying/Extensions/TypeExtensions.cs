namespace JustSaying.Extensions;

internal static class TypeExtensions
{
    public static string ToSimpleName(this Type type) =>
        type.ToString().Replace($"{type.Namespace}.", "");

    /// <summary>
    /// Gets the type's name as C# spells it — <c>CloudEvent&lt;OrderShipped&gt;</c> rather than the CLR's
    /// <c>CloudEvent`1</c> — for logs, span names and error messages. A non-generic type's name is
    /// unchanged.
    /// </summary>
    public static string ToReadableName(this Type type) => Format(type, fullName: false);

    /// <summary>
    /// Gets the type's namespace-qualified name as C# spells it — <c>Lab.CloudEvent&lt;Lab.OrderShipped&gt;</c>
    /// rather than the CLR's <c>Lab.CloudEvent`1[[Lab.OrderShipped, Lab, Version=1.0.0.0, …]]</c>, which
    /// embeds assembly versions. A non-generic type's <see cref="Type.FullName"/> is unchanged.
    /// </summary>
    public static string ToReadableFullName(this Type type) => Format(type, fullName: true);

    private static string Format(Type type, bool fullName)
    {
        if (type.IsArray)
        {
            return Format(type.GetElementType(), fullName) + "[]";
        }

        var name = fullName ? type.FullName ?? type.Name : type.Name;

        if (!type.IsGenericType)
        {
            return name;
        }

        var definitionName = fullName ? type.GetGenericTypeDefinition().FullName ?? type.Name : type.Name;
        var backtick = definitionName.IndexOf('`');
        if (backtick > 0)
        {
            definitionName = definitionName.Substring(0, backtick);
        }

        return $"{definitionName}<{string.Join(", ", type.GetGenericArguments().Select(argument => Format(argument, fullName)))}>";
    }
}
