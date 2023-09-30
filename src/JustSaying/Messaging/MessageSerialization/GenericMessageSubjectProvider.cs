using System.Reflection;
using System.Text.RegularExpressions;
using JustSaying.Extensions;

namespace JustSaying.Messaging.MessageSerialization;

/// <summary>
/// This implementation is suitable for generic types,
/// but using it will break compatibility with versions before IMessageSubjectProvider was introduced
/// </summary>
public class GenericMessageSubjectProvider : IMessageSubjectProvider
{
    private const int MAX_SNS_SUBJECT_LENGTH = 100;

    private static IEnumerable<Type> Flatten(Type type)
    {
        yield return type;
        foreach (var inner in type.GetTypeInfo().GetGenericArguments().SelectMany(Flatten))
        {
            yield return inner;
        }
    }
    public string GetSubjectForType(Type messageType) =>
        string
            .Join("_",
                Flatten(messageType).Select(t => Regex.Replace(t.Name + "_" + t.Namespace, "\\W", "_")))
            .TruncateTo(MAX_SNS_SUBJECT_LENGTH);
}
