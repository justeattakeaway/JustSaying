namespace JustSaying.Extensions;

public static class TypeExtensions
{
    public static string ToSimpleName(this Type type) =>
        type.ToString().Replace($"{type.Namespace}.", "");
}
