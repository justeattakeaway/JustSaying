using System;
using System.Threading.Tasks;
using JustSaying.AwsTools.MessageHandling;
using JustSaying.Models;

namespace JustSaying
{
    internal class DefaultMessageResponseLogger : IMessageResponseLogger
    {
        public Action<MessageResponse, Message> ResponseLogger { get; set; }
        public Func<MessageResponse, Message, Task> ResponseLoggerAsync { get; set; }
    }
}
