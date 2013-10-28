using JustEat.Simples.NotificationStack.Messaging.Monitoring;
using JustEat.StatsD;

namespace JustEat.Simples.NotificationStack.Stack.Monitoring
{
    public class StatsDMessageMonitor : IMessageMonitor
    {
        private readonly StatsDImmediatePublisher _publisher;

        public StatsDMessageMonitor()//StatsDImmediatePublisher publisher
        {
            //_publisher = publisher;
        }

        public void Handled()
        {
            throw new System.NotImplementedException();
        }

        public void HandleException()
        {
            throw new System.NotImplementedException();
        }

        public void HandleTime(long handTimeMs)
        {
            throw new System.NotImplementedException();
        }
    }
}