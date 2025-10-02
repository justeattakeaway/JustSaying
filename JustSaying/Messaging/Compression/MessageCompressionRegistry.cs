using System;
using System.Collections.Generic;
using System.Linq;

namespace JustSaying.Messaging.Compression
{
    /// <summary>
    /// Implements a registry for message compression methods.
    /// </summary>
    public sealed class MessageCompressionRegistry
    {
        private readonly IList<IMessageBodyCompression> _compressions;

        /// <summary>
        /// Initializes a new instance of the <see cref="MessageCompressionRegistry"/> class.
        /// </summary>
        /// <param name="compressions">A list of available compression methods.</param>
        public MessageCompressionRegistry(IList<IMessageBodyCompression> compressions = null)
        {
            _compressions = compressions ?? new List<IMessageBodyCompression>();
        }

        /// <summary>
        /// Retrieves the appropriate compression method based on the content encoding.
        /// </summary>
        /// <param name="contentEncoding">The content encoding identifier.</param>
        /// <returns>An <see cref="IMessageBodyCompression"/> instance for the specified encoding, or null if not found.</returns>
        public IMessageBodyCompression GetCompression(string contentEncoding)
        {
            return _compressions.FirstOrDefault(x => string.Equals(x.ContentEncoding, contentEncoding, StringComparison.OrdinalIgnoreCase));
        }
    }
}
