using System;
using System.ComponentModel;
using JustSaying.TestingFramework;
using Xunit.Abstractions;

namespace Microsoft.Extensions.Logging
{
    /// <summary>
    /// A class containing extension methods for the <see cref="ILoggerFactory"/> interface. This class cannot be inherited.
    /// </summary>

    [EditorBrowsable(EditorBrowsableState.Never)]
    internal static class ILoggerFactoryExtensions
    {
        /// <summary>
        /// Adds an xunit logger to the factory.
        /// </summary>
        /// <param name="factory">The <see cref="ILoggerFactory"/> to use.</param>
        /// <param name="outputHelper">The <see cref="ITestOutputHelper"/> to use.</param>
        /// <returns>
        /// The instance of <see cref="ILoggerFactory"/> specified by <paramref name="factory"/>.
        /// </returns>
        /// <exception cref="ArgumentNullException">
        /// <paramref name="factory"/> or <paramref name="outputHelper"/> is <see langword="null"/>.
        /// </exception>
        public static ILoggerFactory AddXunit(this ILoggerFactory factory, ITestOutputHelper outputHelper)
        {
            if (factory == null)
            {
                throw new ArgumentNullException(nameof(factory));
            }

            if (outputHelper == null)
            {
                throw new ArgumentNullException(nameof(outputHelper));
            }

            var provider = new XunitLoggerProvider(outputHelper);

            factory.AddProvider(provider);

            return factory;
        }
    }
}
