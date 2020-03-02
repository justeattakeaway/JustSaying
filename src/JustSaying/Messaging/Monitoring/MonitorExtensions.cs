namespace JustSaying.Messaging.Monitoring
{
    public static class MonitorExtensions
    {
        public static Operation MeasureThrottle(this IMessageMonitor messageMonitor)
            => new Operation(messageMonitor, (duration, monitor) =>
            {
                monitor.IncrementThrottlingStatistic();
                monitor.HandleThrottlingTime(duration);
            });

        public static Operation MeasureHandler(this IMessageMonitor messageMonitor)
            => new Operation(messageMonitor, (duration, monitor) => monitor.HandleTime(duration));

        public static Operation MeasureReceive(this IMessageMonitor messageMonitor, string queueName, string region)
            => new Operation(messageMonitor,
                (duration, monitor) => monitor.ReceiveMessageTime(duration, queueName, region));

        public static Operation MeasurePublish(this IMessageMonitor messageMonitor)
            => new Operation(messageMonitor, (duration, monitor) => monitor.PublishMessageTime(duration));
    }
}
