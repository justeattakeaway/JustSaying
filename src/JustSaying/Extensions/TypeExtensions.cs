namespace JustSaying.Extensions;

static class TypeExtensions
{
    public static string ToSimpleName(this Type type) =>
        type.ToString().Replace($"{type.Namespace}.", "");
}
