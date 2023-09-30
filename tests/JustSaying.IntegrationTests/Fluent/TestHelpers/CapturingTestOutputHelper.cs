using System.Text;

namespace JustSaying.IntegrationTests.Fluent.Subscribing;

public class CapturingTestOutputHelper(ITestOutputHelper inner) : ITestOutputHelper
{
    private readonly StringBuilder _sb = new();

    public string Output => _sb.ToString();

    public void WriteLine(string message)
    {
        _sb.AppendLine(message);
        inner.WriteLine(message);
    }

    public void WriteLine(string format, params object[] args)
    {
        _sb.AppendLine(string.Format(format, args));
        inner.WriteLine(format, args);
    }
}
