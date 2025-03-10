using JustSaying.Models;

namespace JustSaying.AwsTools.QueueCreation;

public class SnsWriteConfiguration
{
    public ServerSideEncryption Encryption { get; set; }

    /// <summary>
    /// Extension point enabling custom error handling on a per notification basis, including ability handle raised exceptions.
    /// </summary>
    /// <returns>Boolean indicating whether the exception has been handled</returns>
    public Func<Exception, Message, bool> HandleException { get; set; }

    public bool IsFifoTopic { get; set; }
}
