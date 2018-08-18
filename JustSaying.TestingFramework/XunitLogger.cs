using System;
using Microsoft.Extensions.Logging;
using Xunit.Abstractions;

namespace JustSaying.TestingFramework
{
    /// <summary>
    /// A class representing an <see cref="ILogger"/> to use with xunit. This class cannot be inherited.
    /// </summary>
    internal sealed class XunitLogger : ILogger
    {
        private readonly string _categoryName;
        private readonly ITestOutputHelper _outputHelper;

        internal XunitLogger(string categoryName, ITestOutputHelper outputHelper)
        {
            _categoryName = categoryName;
            _outputHelper = outputHelper;
        }

        /// <inheritdoc />
        public IDisposable BeginScope<TState>(TState state) => null;

        /// <inheritdoc />
        public bool IsEnabled(LogLevel logLevel) => true;

        /// <inheritdoc />
        public void Log<TState>(LogLevel logLevel, EventId eventId, TState state, Exception exception, Func<TState, Exception, string> formatter)
        {
            _outputHelper.WriteLine($"[{DateTimeOffset.Now:u}] [{logLevel}:{_categoryName}:{eventId}] {formatter(state, exception)}");
        }
    }
}
