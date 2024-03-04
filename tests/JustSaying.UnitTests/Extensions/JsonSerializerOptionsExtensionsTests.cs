using System.Text.Json;
using System.Text.Json.Serialization;
using System.Text.Json.Serialization.Metadata;
using JustSaying.Models;
using JustSaying.Extensions;

namespace JustSaying.UnitTests.Extensions;

public sealed partial class JsonSerializerOptionsExtensionsTests
{
    private readonly JsonSerializerOptions _sut = new()
    {
        TypeInfoResolver = TestSerializerContext.Default,
    };

    [Fact]
    public void GetTypeInfo_ReturnsExpectedValue_WhenOptionsContainTypeInfo()
    {
        var result = _sut.GetTypeInfo<TestContainedClass>();
        result.ShouldBeOfType<JsonTypeInfo<TestContainedClass>>();
    }

    [Fact]
    public void GetTypeInfo_ThrowsArgumentException_WhenOptionsDoNotContainTypeInfo()
    {
        var testAction = () => _sut.GetTypeInfo<TestNotContainedClass>();
        testAction.ShouldThrow<NotSupportedException>();
    }

    public class TestContainedClass : Message;

    public class TestNotContainedClass : Message;

    [JsonSerializable(typeof(TestContainedClass))]
    public sealed partial class TestSerializerContext : JsonSerializerContext;
}
