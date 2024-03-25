namespace JustSaying;

internal static class Constants
{
    internal const string SerializationUnreferencedCodeMessage = "JSON serialization and deserialization might require types that cannot be statically analyzed. Use the generic SystemTextJsonSerializer<T>.";
    internal const string SerializationDynamicCodeMessage = "JSON serialization and deserialization might require types that cannot be statically analyzed. Use the generic SystemTextJsonSerializer<T>.";
}
