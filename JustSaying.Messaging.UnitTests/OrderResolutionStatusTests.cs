using System;
using System.Linq;
using JustSaying.Messaging.Messages;
using NUnit.Framework;

namespace UnitTests
{
    [TestFixture]
    public class OrderResolutionStatusTests
    {
        [Test]
        public void EveryStatusIsEitherInTheAcceptedOrCancelledGroup()
        {
            var values = Enum.GetValues(typeof (OrderResolutionStatus));

            foreach (var value in values)
            {
              var status = (OrderResolutionStatus) value;

                Assert.IsTrue(
                    OrderResolutionStatusGroups.AcceptedStates.Contains(status) ^
                    OrderResolutionStatusGroups.CancelledStates.Contains(status),
                    "The status {0} either does not exist in a group, or exists in both groups.", status);


            }
        }
    }
}
