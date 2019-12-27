using System;
using System.Collections.Generic;
using JustSaying.Extensions;
using JustSaying.TestingFramework;
using Shouldly;
using Xunit;

namespace JustSaying.UnitTests
{
    public class TypeExtensionTests
    {
        [Fact]
        public void WhenGeneratingTopicName_ForNonGenericType_ThenTheCorrectNameShouldBeReturned()
        {
            // Arrange + Act
            var result = typeof(SimpleMessage).ToTopicName();

            // Assert
            result.ShouldBe("simplemessage");
        }

        [Fact]
        public void WhenGeneratingTopicName_ForGenericType_ThenTheCorrectNameShouldBeReturned()
        {
            // Arrange + Act
            var result = typeof(List<List<string>>).ToTopicName();

            // Assert
            result.ShouldBe("listliststring");
        }

        [Fact]
        public void WhenGeneratingTopicName_ForTypeWithLongName_ThenTheLengthShouldBe256()
        {
            // Arrange + Act
            var result =
                typeof(Tuple<TypeWithAReallyReallyReallyLongClassNameThatShouldExceedTheMaximumLengthOfAnAwsResourceName
                        , TypeWithAReallyReallyReallyLongClassNameThatShouldExceedTheMaximumLengthOfAnAwsResourceName,
                        TypeWithAReallyReallyReallyLongClassNameThatShouldExceedTheMaximumLengthOfAnAwsResourceName>)
                    .ToTopicName();

            // Arrange
            result.Length.ShouldBe(256);
        }

        [Fact]
        public void WhenGeneratingQueueName_ForNonGenericType_ThenTheCorrectNameShouldBeReturned()
        {
            // Arrange + Act
            var result = typeof(SimpleMessage).ToQueueName();

            // Assert
            result.ShouldBe("simplemessage");
        }

        [Fact]
        public void WhenGeneratingQueueName_ForGenericType_ThenTheCorrectNameShouldBeReturned()
        {
            // Arrange + Act
            var result = typeof(List<string>).ToQueueName();

            // Assert
            result.ShouldBe("liststring");
        }

        [Fact]
        public void WhenGeneratingQueueName_ForTypeWithLongName_ThenTheLengthShouldBe80()
        {
            // Arrange + Act
            var result =
                typeof(TypeWithAReallyReallyReallyLongClassNameThatShouldExceedTheMaximumLengthOfAnAwsResourceName)
                    .ToQueueName();

            // Assert
            result.Length.ShouldBe(80);
        }

        public class TypeWithAReallyReallyReallyLongClassNameThatShouldExceedTheMaximumLengthOfAnAwsResourceName
        {
        }
    }
}
