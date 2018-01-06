using System;
using Amazon.SimpleNotificationService.Model;

namespace JustSaying.AwsTools.QueueCreation
{
    public class SnsWriteConfiguration
    {
        /// <summary>
        /// Extension point allows custom error handling, including ability to specify whether exception has been explictly handled by consumer.
        /// </summary>
        /// <returns>Boolean to indicate whether the exception has already been handled by the consumer</returns>
        public Func<Exception, PublishRequest, bool> OnException { get; set; }
    }
}
