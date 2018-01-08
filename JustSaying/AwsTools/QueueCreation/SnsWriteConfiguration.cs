using System;
using Amazon.SimpleNotificationService.Model;

namespace JustSaying.AwsTools.QueueCreation
{
    public class SnsWriteConfiguration
    {
        /// <summary>
        /// Extension point enabling custom error handling on a per notification basis, including ability handle the exception.
        /// </summary>
        /// <returns>Boolean indicating whether the exception has been handled</returns>
        public Func<Exception, bool> HandleException { get; set; }
    }
}
