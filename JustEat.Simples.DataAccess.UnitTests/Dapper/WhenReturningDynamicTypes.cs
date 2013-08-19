using System.Collections.Generic;
using System.Linq;
using Microsoft.CSharp.RuntimeBinder;
using NUnit.Framework;

namespace JustEat.Simples.DataAccess.UnitTests.Dapper
{
    [TestFixture]
    public class WhenReturningDynamicTypes
    {
        [Test]
        [ExpectedException(typeof(RuntimeBinderException))]
        public void AndCallingFirstAsExtensionMethod()
        {
            dynamic list = new List<int> {1, 2, 3};
            list.First();
        }

        [Test]
        public void AndCallingFirstFromEnumerableClass()
        {
            dynamic list = new List<int> { 1, 2, 3 };
            Enumerable.First(list);
        }
    }
}
