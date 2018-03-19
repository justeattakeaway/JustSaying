using System;
using System.Threading.Tasks;
using JustSaying.AwsTools.MessageHandling;
using JustSaying.Models;

namespace JustSaying
{
    public interface IMessageResponseLogger
    {
        /// <summary>
        /// Capture response of message after it has been sent.
        /// </summary>
        Action<MessageResponse, Message> ResponseLogger { get; set; }

        /// <summary>
        /// Asynchronously capture response of message after it has been sent.
        /// </summary>
        Func<MessageResponse, Message, Task> ResponseLoggerAsync { get; set; }
    }
}
