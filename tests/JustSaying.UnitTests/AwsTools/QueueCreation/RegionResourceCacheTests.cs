using JustSaying.AwsTools.QueueCreation;
using Shouldly;
using Xunit;

namespace JustSaying.UnitTests.AwsTools.QueueCreation
{
    public class RegionResourceCacheTests
    {
        [Fact]
        public void EmptyRegionResourceCacheWillReturnNulls()
        {
            var cache = new RegionResourceCache<string>();
            var value = cache.TryGetFromCache("eu-west1", "someKey");

            value.ShouldBeNull();
        }

        [Fact]
        public void KeysAreCaseInsensitive()
        {
            var cache = new RegionResourceCache<string>();
            cache.AddToCache("eu-west1", "testKey", "42");

            var value = cache.TryGetFromCache("EU-West1", "TESTKEY");

            value.ShouldBe("42");
        }

        [Fact]
        public void CanRetrieveValueByRegionAndKey()
        {
            var cache = new RegionResourceCache<string>();
            cache.AddToCache("eu-west1", "testKey", "42");

            var value = cache.TryGetFromCache("eu-west1", "testKey");

            value.ShouldBe("42");
        }

        [Fact]
        public void RegionDifferenceWillNotMatchValue()
        {
            var cache = new RegionResourceCache<string>();
            cache.AddToCache("eu-west1", "testKey", "42");

            var value = cache.TryGetFromCache("ap-southeast2", "testKey");

            value.ShouldBeNull();
        }

        [Fact]
        public void KeyDifferenceWillNotMatchValue()
        {
            var cache = new RegionResourceCache<string>();
            cache.AddToCache("eu-west1", "testKey", "42");

            var value = cache.TryGetFromCache("eu-west1", "otherKey");

            value.ShouldBeNull();
        }

        [Fact]
        public void CanRetrieveValueOutOfMany()
        {
            var cache = new RegionResourceCache<string>();
            cache.AddToCache("eu-west1", "testKey", "42");
            cache.AddToCache("ap-southeast2", "testKey", "12");
            cache.AddToCache("eu-west1", "otherKey", "+");
            cache.AddToCache("ap-southeast2", "otherKey", "-");
            cache.AddToCache("eu-west1", "thirdKey", "value");

            var value = cache.TryGetFromCache("ap-southeast2", "testKey");

            value.ShouldBe("12");
        }
    }
}
