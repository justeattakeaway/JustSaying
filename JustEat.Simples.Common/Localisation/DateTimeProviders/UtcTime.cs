using System;

namespace JustEat.Simples.Common.Localisation.DateTimeProviders
{
    public class UtcTime : ITime
    {
        public DateTime Now { get { return DateTime.UtcNow; } }
    }
}