using System;
using JustSaying.AwsTools.QueueCreation;
using JustSaying.Models;

namespace JustSaying.Fluent
{
    /// <summary>
    /// A class representing a builder for configuring instances of <see cref="SnsWriteConfiguration"/>. This class cannot be inherited.
    /// </summary>
    public sealed class SnsWriteConfigurationBuilder
    {
        /// <summary>
        /// Configures the specified <see cref="SnsWriteConfiguration"/>.
        /// </summary>
        /// <param name="config">The configuration to configure.</param>
        internal void Configure(SnsWriteConfiguration config)
        {
        }
    }
}
