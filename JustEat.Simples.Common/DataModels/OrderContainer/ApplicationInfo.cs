namespace JustEat.Simples.Common.DataModels.OrderContainer
{
    /// <summary>
    /// Describe client application.
    /// </summary>
    public class ApplicationInfo
    {
        /// <summary>
        /// User agent used during the call.
        /// </summary>
        public string UserAgent { get; set; }

        public string ClientIp { get; set; }

        /// <summary>
        /// Name of the application (client).
        /// </summary>
        public string ApplicationName { get; set; }

        /// <summary>
        /// Version of the application (client).
        /// </summary>
        public string ApplicationVersion { get; set; }
    }
}