using System;

namespace JustEat.Simples.Common.Localisation.DateTimeProviders
{
    public interface ITime
    {
        DateTime Now { get; }
    }
}
