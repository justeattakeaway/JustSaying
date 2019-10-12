using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using NSubstitute;
using Shouldly;
using Xunit;

namespace JustSaying.UnitTests.JustSayingBus
{
    public class WhenUsingMultipleRegions : GivenAServiceBus
    {
        protected override void Given()
        {
            base.Given();
            Config.Regions.Returns(new List<string> { "region1", "region2" });
        }

        protected override Task WhenAsync()
        {
            return Task.CompletedTask;
        }

        [Fact]
        public void RegionsAreReturnedInTheInterrogationResult()
        {
            var response = SystemUnderTest.WhatDoIHave();

            response.Regions.Count().ShouldBe(2);
            response.Regions.ShouldContain("region1");
            response.Regions.ShouldContain("region2");
        }
    }
}
