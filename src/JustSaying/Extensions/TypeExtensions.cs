namespace JustSaying.Extensions;

internal static class TypeExtensions
{
    public static string ToSimpleName(this Type type) =>
        type.ToString().Replace($"{type.Namespace}.", "");
}
