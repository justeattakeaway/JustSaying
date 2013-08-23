using System;
using JustEat.Simples.Common.Localisation.DateTimeProviders;
using JustEat.Testing;
using NSubstitute;
using NUnit.Framework;

namespace JustEat.Simples.Common.UnitTests.Localisation.DateTimes
{
    public class WhenGettingOperatingTimeOutsideSummerTime : BehaviourTest<OperatingTime>
    {
        private DateTime _result;
        //in outside summer time UK is on UTC, relative difference between UK and Toronto time does not change
        private readonly DateTime _now = new DateTime(2011, 11, 8, 12, 12, 10, DateTimeKind.Utc);
        private readonly ITime _utcTime = Substitute.For<ITime>();

        protected override OperatingTime CreateSystemUnderTest()
        {
            return new OperatingTime("Eastern Standard Time", _utcTime);
        }

        protected override void Given()
        {
            _utcTime.Now.Returns(_now);
        }

        protected override void When()
        {
            _result = SystemUnderTest.Now;
        }

        [Then]
        public void OperatingTimeDependsOnCurrentTimeAndConfigSetting()
        {
            Assert.AreEqual(new DateTime(2011, 11, 8, 7, 12, 10), _result);

        }
    }
}