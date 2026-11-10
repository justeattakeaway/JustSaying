#nullable enable

using System.Diagnostics;
using System.Text.Json;
using System.Text.Json.Serialization;
using JustSaying.AwsTools;
using JustSaying.TestingFramework;
using LocalSqsSnsMessaging;
using Microsoft.Extensions.DependencyInjection;

namespace JustSaying.AsyncApi.Tests;

/// <summary>
/// A recursive payload type is described once, in the document's component schemas, and referenced
/// wherever it appears, rather than inlined at every self-reference (which grows exponentially with
/// the number of self-references).
/// </summary>
public class WhenAPayloadTypeIsRecursive
{
    public sealed class TreeNode
    {
        public string Name { get; set; } = "";

        public TreeNode? A { get; set; }

        public TreeNode? B { get; set; }

        public TreeNode? C { get; set; }

        public TreeNode? D { get; set; }

        public TreeNode? E { get; set; }

        public List<TreeNode> Children { get; set; } = [];
    }

    public sealed record TreeChanged(TreeNode Tree, TreeNode? Previous);

    [JsonPolymorphic(TypeDiscriminatorPropertyName = "kind")]
    [JsonDerivedType(typeof(Circle), "circle")]
    [JsonDerivedType(typeof(Group), "group")]
    public abstract class Shape
    {
    }

    public sealed class Circle : Shape
    {
        public double Radius { get; set; }
    }

    public sealed class Group : Shape
    {
        public List<Shape> Members { get; set; } = [];
    }

    public sealed record ShapeDrawn(Shape Shape);

    public sealed class Other
    {
        public sealed class TreeNode
        {
            public TreeNode? Parent { get; set; }
        }
    }

    private static async Task<string> GenerateAsync(Action<JustSaying.Fluent.PublicationsBuilder> publications)
    {
        var services = new ServiceCollection();
        services.AddLogging();
        services.AddSingleton<IAwsClientFactory>(new LocalAwsClientFactory(new InMemoryAwsBus()));
        services.AddJustSaying((config) =>
        {
            config.Messaging((x) => x.WithRegion("eu-west-1"));
            config.Publications(publications);
        });
        services.AddJustSayingAsyncApi();

        var provider = services.BuildServiceProvider().GetRequiredService<IAsyncApiDocumentProvider>();
        using var writer = new StringWriter();
        await provider.GenerateAsync(provider.GetDocumentNames()[0], writer);
        return writer.ToString();
    }

    private static JsonElement Payload(JsonDocument document, string channel)
        => document.RootElement.GetProperty("channels").GetProperty(channel).GetProperty("messages").EnumerateObject().First().Value.GetProperty("payload");

    [Test]
    public async Task ATypeWithManySelfReferencesIsDescribedOnce()
    {
        var stopwatch = Stopwatch.StartNew();
        var json = await GenerateAsync((x) => x.WithTopic<TreeNode>());
        stopwatch.Stop();

        // Inlined, six self-references produced no document within 90 seconds.
        await Assert.That(stopwatch.Elapsed).IsLessThan(TimeSpan.FromSeconds(10));
        await Assert.That(json.Length).IsLessThan(10_000);

        using var document = JsonDocument.Parse(json);
        await Assert.That(Payload(document, "treenode").GetProperty("$ref").GetString()).IsEqualTo("#/components/schemas/TreeNode");

        var treeNode = document.RootElement.GetProperty("components").GetProperty("schemas").GetProperty("TreeNode");
        await Assert.That(treeNode.GetProperty("type").GetString()).IsEqualTo("object");
        await Assert.That(treeNode.GetProperty("properties").GetProperty("Name").GetProperty("type").GetString()).IsEqualTo("string");

        // A nullable self-reference is the component or null; a collection of them references it too.
        var a = treeNode.GetProperty("properties").GetProperty("A").GetProperty("anyOf");
        await Assert.That(a[0].GetProperty("$ref").GetString()).IsEqualTo("#/components/schemas/TreeNode");
        await Assert.That(a[1].GetProperty("type").GetString()).IsEqualTo("null");

        var children = treeNode.GetProperty("properties").GetProperty("Children");
        await Assert.That(children.GetProperty("items").GetProperty("$ref").GetString()).IsEqualTo("#/components/schemas/TreeNode");
    }

    [Test]
    public async Task EveryUseOfARecursiveTypeReferencesTheSameComponent()
    {
        using var document = JsonDocument.Parse(await GenerateAsync((x) =>
        {
            x.WithTopic<TreeNode>();
            x.WithTopic<TreeChanged>();
        }));

        var properties = Payload(document, "treechanged").GetProperty("properties");
        await Assert.That(properties.GetProperty("Tree").GetProperty("$ref").GetString()).IsEqualTo("#/components/schemas/TreeNode");
        await Assert.That(properties.GetProperty("Previous").GetProperty("anyOf")[0].GetProperty("$ref").GetString()).IsEqualTo("#/components/schemas/TreeNode");

        var schemas = document.RootElement.GetProperty("components").GetProperty("schemas");
        await Assert.That(schemas.EnumerateObject().Select((s) => s.Name).ToList()).IsEquivalentTo(["TreeNode"]);
    }

    [Test]
    public async Task ARecursivePolymorphicTypeIsDescribedOnce()
    {
        using var document = JsonDocument.Parse(await GenerateAsync((x) => x.WithTopic<ShapeDrawn>()));

        await Assert.That(Payload(document, "shapedrawn").GetProperty("properties").GetProperty("Shape").GetProperty("$ref").GetString())
            .IsEqualTo("#/components/schemas/Shape");

        var shape = document.RootElement.GetProperty("components").GetProperty("schemas").GetProperty("Shape");
        var group = shape.GetProperty("anyOf").EnumerateArray().Single((s) => s.GetProperty("properties").GetProperty("kind").GetProperty("const").GetString() == "group");
        await Assert.That(group.GetProperty("properties").GetProperty("Members").GetProperty("items").GetProperty("$ref").GetString())
            .IsEqualTo("#/components/schemas/Shape");
    }

    [Test]
    public async Task TypesWithTheSameNameGetDistinctComponents()
    {
        using var document = JsonDocument.Parse(await GenerateAsync((x) =>
        {
            x.WithTopic<TreeNode>();
            x.WithTopic<Other.TreeNode>((t) => t.WithTopicName("other-tree"));
        }));

        await Assert.That(Payload(document, "treenode").GetProperty("$ref").GetString()).IsEqualTo("#/components/schemas/TreeNode");
        await Assert.That(Payload(document, "other-tree").GetProperty("$ref").GetString()).IsEqualTo("#/components/schemas/TreeNode-2");

        var other = document.RootElement.GetProperty("components").GetProperty("schemas").GetProperty("TreeNode-2");
        await Assert.That(other.GetProperty("properties").GetProperty("Parent").GetProperty("anyOf")[0].GetProperty("$ref").GetString())
            .IsEqualTo("#/components/schemas/TreeNode-2");
    }

    [Test]
    public async Task TheDocumentIsTheSameEveryTime()
    {
        static Task<string> Generate() => GenerateAsync((x) =>
        {
            x.WithTopic<TreeChanged>();
            x.WithTopic<ShapeDrawn>();
        });

        await Assert.That(await Generate()).IsEqualTo(await Generate());
    }
}
