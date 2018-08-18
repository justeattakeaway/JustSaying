using System.ComponentModel;
using Microsoft.Extensions.Logging;

namespace Xunit.Abstractions
{
    /// <summary>
    /// A class containing extension methods for the <see cref="ITestOutputHelper"/> interface. This class cannot be inherited.
    /// </summary>
    [EditorBrowsable(EditorBrowsableState.Never)]
    public static class ITestOutputHelperExtensions
    {
        /// <summary>
        /// Returns an <see cref="ILoggerFactory"/> that logs to the output helper.
        /// </summary>
        /// <param name="outputHelper">The <see cref="ITestOutputHelper"/> to create the logger factory from.</param>
        /// <returns>
        /// An <see cref="ILoggerFactory"/> that writes messages to the test output helper.
        /// </returns>
        public static ILoggerFactory AsLoggerFactory(this ITestOutputHelper outputHelper)
            => new LoggerFactory().AddXunit(outputHelper);
    }
}
