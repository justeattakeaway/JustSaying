#nullable enable
using Microsoft.Extensions.Logging;

namespace JustSaying.TestingFramework;

public static class TextWriterLoggingExtensions
{
    public static ILoggerFactory ToLoggerFactory(this TextWriter writer)
    {
        return LoggerFactory.Create(builder => builder.AddTextWriter(writer));
    }

    public static ILogger<T> ToLogger<T>(this TextWriter writer)
    {
        return writer.ToLoggerFactory().CreateLogger<T>();
    }

    public static ILoggingBuilder AddTextWriter(this ILoggingBuilder builder, TextWriter writer, Action<TextWriterLoggerOptions>? configure = null)
    {
        var options = new TextWriterLoggerOptions();
        configure?.Invoke(options);
        builder.AddProvider(new TextWriterLoggerProvider(writer, options));
        if (options.Filter != null)
        {
            builder.AddFilter((category, level) => options.Filter(category, level));
        }
        return builder;
    }
}

public class TextWriterLoggerOptions
{
    public bool IncludeScopes { get; set; }
    public Func<string?, LogLevel, bool>? Filter { get; set; }
}

public sealed class TextWriterLoggerProvider : ILoggerProvider
{
    private readonly TextWriter _writer;

    public TextWriterLoggerProvider(TextWriter writer, TextWriterLoggerOptions? options = null)
    {
        _writer = writer;
    }

    public ILogger CreateLogger(string categoryName) => new TextWriterLogger(_writer, categoryName);

    public void Dispose() { }
}

internal sealed class TextWriterLogger(TextWriter writer, string categoryName) : ILogger
{
    public IDisposable? BeginScope<TState>(TState state) where TState : notnull => null;

    public bool IsEnabled(LogLevel logLevel) => logLevel != LogLevel.None;

    public void Log<TState>(LogLevel logLevel, EventId eventId, TState state, Exception? exception, Func<TState, Exception?, string> formatter)
    {
        if (!IsEnabled(logLevel)) return;

        var message = formatter(state, exception);
        lock (writer)
        {
            writer.WriteLine($"[{logLevel}] {categoryName}: {message}");
            if (exception != null)
            {
                writer.WriteLine(exception.ToString());
            }
        }
    }
}
