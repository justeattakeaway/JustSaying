using System.Text;
using Xunit.Abstractions;

namespace JustSaying.IntegrationTests.Fluent.Subscribing;

public class CapturingTestOutputHelper : ITestOutputHelper
{
    private readonly ITestOutputHelper _inner;
    private readonly StringBuilder _sb;

    public string Output => _sb.ToString();

    public CapturingTestOutputHelper(ITestOutputHelper inner)
    {
        _inner = inner;
        _sb = new StringBuilder();
    }

    public void WriteLine(string message)
    {
        _sb.AppendLine(message);
        _inner.WriteLine(message);
    }

    public void WriteLine(string format, params object[] args)
    {
        _sb.AppendLine(string.Format(format, args));
        _inner.WriteLine(format, args);
    }
}