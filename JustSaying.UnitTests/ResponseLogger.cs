using System;
using System.Threading.Tasks;
using JustSaying.AwsTools.MessageHandling;

namespace JustSaying.UnitTests
{
    public static class ResponseLogger
    {
        public static Func<MessageResponse, Task> NoOp { get; } = _ => Task.CompletedTask;
    }
}
