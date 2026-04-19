#nullable enable
using System.Text;

namespace JustSaying.IntegrationTests.Fluent.Subscribing;

public class CapturingTestOutputHelper(TextWriter inner) : TextWriter
{
    private readonly StringBuilder _sb = new();

    public string Output => _sb.ToString();

    public override Encoding Encoding => inner.Encoding;

    public override void Write(string? value)
    {
        _sb.Append(value);
        inner.Write(value);
    }

    public override void WriteLine(string? value)
    {
        _sb.AppendLine(value);
        inner.WriteLine(value);
    }
}
