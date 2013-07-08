namespace JustEat.Simples.DataAccess
{
    public interface ISqlMonitoringService
    {
        void SqlException();
        void SqlDeadlockException();
    }
}