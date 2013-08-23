using System;

namespace JustEat.Simples.Common.Localisation.DateTimeProviders
{
    public class OperatingTime : ITime
    {
        private readonly string _timeZoneId;
        private readonly ITime _time;

        public OperatingTime(string timeZoneId, ITime time)
        {
            _timeZoneId = timeZoneId;
            _time = time;
        }

        public DateTime Now
        {
            get
            {
                var timeZoneInfo = TimeZoneInfo.FindSystemTimeZoneById(_timeZoneId);
                DateTimeOffset localTimeNow = TimeZoneInfo.ConvertTimeFromUtc(_time.Now, timeZoneInfo);
                return localTimeNow.DateTime;
            }
        }
    }
}