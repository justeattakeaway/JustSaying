using System;

namespace JustEat.Simples.DataAccess
{
    public class EventingConfiguration
    {
        public Action SqlException;
        public Action SqlDeadlockException;
    }
}