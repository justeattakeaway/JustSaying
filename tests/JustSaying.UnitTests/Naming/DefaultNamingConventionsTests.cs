using System;
using System.Collections.Generic;
using JustSaying.Naming;
using JustSaying.TestingFramework;
using Shouldly;
using Xunit;

namespace JustSaying.UnitTests.Naming
{
    public class DefaultNamingConventionsTests
    {
        private readonly DefaultNamingConventions Sut = new DefaultNamingConventions();

        [Fact]
        public void WhenGeneratingTopicName_ForNonGenericType_ThenTheCorrectNameShouldBeReturned()
        {
            // Arrange + Act
            var result = Sut.TopicName<SimpleMessage>(null);

            // Assert
            result.ShouldBe("simplemessage");
        }

        [Fact]
        public void WhenGeneratingTopicName_ForGenericType_ThenTheCorrectNameShouldBeReturned()
        {
            // Arrange + Act
            var result = Sut.TopicName<List<List<string>>>(null);

            // Assert
            result.ShouldBe("listliststring");
        }

        [Fact]
        public void WhenGeneratingTopicName_ForTypeWithLongName_ThenTheLengthShouldBe256()
        {
            // Arrange + Act
            var result = Sut
                .TopicName<Tuple<
                    TypeWithAReallyReallyReallyLongClassNameThatShouldExceedTheMaximumLengthOfAnAwsResourceName
                    , TypeWithAReallyReallyReallyLongClassNameThatShouldExceedTheMaximumLengthOfAnAwsResourceName,
                    TypeWithAReallyReallyReallyLongClassNameThatShouldExceedTheMaximumLengthOfAnAwsResourceName>>(null);

            // Arrange
            result.Length.ShouldBe(256);
        }

        [Fact]
        public void WhenGeneratingQueueName_ForNonGenericType_ThenTheCorrectNameShouldBeReturned()
        {
            // Arrange + Act
            var result = Sut.QueueName<SimpleMessage>(null);

            // Assert
            result.ShouldBe("simplemessage");
        }

        [Fact]
        public void WhenGeneratingQueueName_ForGenericType_ThenTheCorrectNameShouldBeReturned()
        {
            // Arrange + Act
            var result = Sut.QueueName<List<string>>(null);

            // Assert
            result.ShouldBe("liststring");
        }

        [Fact]
        public void WhenGeneratingQueueName_ForTypeWithLongName_ThenTheLengthShouldBe80()
        {
            // Arrange + Act
            var result =
                Sut.QueueName<
                    TypeWithAReallyReallyReallyLongClassNameThatShouldExceedTheMaximumLengthOfAnAwsResourceName>(null);

            // Assert
            result.Length.ShouldBe(80);
        }

        public class TypeWithAReallyReallyReallyLongClassNameThatShouldExceedTheMaximumLengthOfAnAwsResourceName
        {
        }
    }
}
