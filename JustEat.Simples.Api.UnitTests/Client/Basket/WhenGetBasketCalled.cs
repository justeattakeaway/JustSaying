using System.Collections.Generic;
using JustEat.Simples.Api.Client;
using JustEat.Simples.Api.Client.Basket;
using JustEat.Simples.Api.Client.Basket.Models;
using JustEat.Testing;
using NSubstitute;


namespace JustEat.Simples.Api.UnitTests.Client.Basket
{
    public class WhenGetBasketCalled : BehaviourTest<BasketApi>
    {
        private OrderBasketResponse _actual;
        private OrderBasketResponse _expected;

        protected override void Given()
        {
        }

        //protected override BasketApi CreateSystemUnderTest()
        //{
        //    return _mock.Object;
        //}

        protected override void When()
        {

            SystemUnderTest.GetBasket("ASDfasdf");
        }

        //[Then]
        //public void ResultIsExpectedValue()
        //{
        //}
    }
}