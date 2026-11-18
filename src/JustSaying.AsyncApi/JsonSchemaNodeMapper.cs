using System.Text.Json;
using System.Text.Json.Nodes;
using System.Text.Json.Schema;
using System.Text.Json.Serialization;
using System.Text.Json.Serialization.Metadata;
using ByteBard.AsyncAPI.Models;

namespace JustSaying.AsyncApi;

/// <summary>
/// Maps the JSON Schema produced by <see cref="JsonSchemaExporter"/> onto the AsyncAPI schema
/// object model. An instance maps the schema of one exported type: export with
/// <see cref="ExporterOptions"/>, then <see cref="Map(JsonNode)"/> the result.
/// </summary>
/// <remarks>
/// The exporter describes a recursive type with "$ref" JSON Pointers into the exported schema
/// itself (for example "#/properties/Left"), which would not resolve once embedded in the wider
/// AsyncAPI document. Inlining them instead grows exponentially with the number of
/// self-references, so every type that takes part in a cycle is added to the document's
/// component schemas once and referenced from there.
/// </remarks>
internal sealed class JsonSchemaNodeMapper
{
    // Marks each exported schema node with the index of the type it describes in _types. Unknown
    // keywords are never mapped, so the marker does not reach the document.
    private const string TypeKeyword = "$justSayingType";

    private readonly List<JsonTypeInfo> _types = [];
    private readonly HashSet<Type> _recursiveTypes = [];
    private readonly JsonIgnoreCondition _ignoreCondition;
    private readonly Func<Type, Func<AsyncApiJsonSchema>, string> _addComponent;
    private JsonNode _root;

    /// <summary>
    /// Initializes a new instance of the <see cref="JsonSchemaNodeMapper"/> class.
    /// </summary>
    /// <param name="serializerOptions">The options the payload is serialized with, which decide what is written.</param>
    /// <param name="addComponent">
    /// Adds the schema of a type to the document's component schemas, unless it is already there, and
    /// returns the reference to it. The schema is created by the delegate passed, which may itself
    /// reference the type being added.
    /// </param>
    public JsonSchemaNodeMapper(JsonSerializerOptions serializerOptions, Func<Type, Func<AsyncApiJsonSchema>, string> addComponent)
    {
        _ignoreCondition = serializerOptions.DefaultIgnoreCondition;
        _addComponent = addComponent;
        ExporterOptions = new JsonSchemaExporterOptions()
        {
            TreatNullObliviousAsNonNullable = true,
            TransformSchemaNode = MarkType,
        };
    }

    /// <summary>
    /// Gets the options to export the schema with.
    /// </summary>
    public JsonSchemaExporterOptions ExporterOptions { get; }

    public AsyncApiJsonSchema Map(JsonNode root)
    {
        _root = root;
        FindRecursiveTypes(root, "#", []);
        return Map(root, new HashSet<string>(StringComparer.Ordinal));
    }

    private JsonNode MarkType(JsonSchemaExporterContext context, JsonNode node)
    {
        // A derived type's schema inside a polymorphic base is a fragment of the base's schema
        // (it has no "type" and carries the discriminator), so only standalone schemas are marked.
        if (node is JsonObject obj && context.BaseTypeInfo == null)
        {
            obj[TypeKeyword] = _types.Count;
            _types.Add(context.TypeInfo);
        }

        return node;
    }

    /// <summary>
    /// Finds the types that take part in a cycle: every object type on the path from a "$ref" up
    /// to the schema it points back to. A "$ref" that points elsewhere (the exporter reuses an
    /// earlier schema of a recursive type) marks the type it points to.
    /// </summary>
    private void FindRecursiveTypes(JsonNode node, string pointer, List<(string Pointer, JsonNode Node)> ancestors)
    {
        if (node is JsonArray array)
        {
            for (int i = 0; i < array.Count; i++)
            {
                FindRecursiveTypes(array[i], $"{pointer}/{i}", ancestors);
            }

            return;
        }

        if (node is not JsonObject obj)
        {
            return;
        }

        if (RefPointer(obj) is { } target)
        {
            int start = ancestors.FindIndex((a) => a.Pointer == target);
            var cycle = start >= 0 ? ancestors.Skip(start).Select((a) => a.Node) : [ResolvePointer(target)];

            foreach (var member in cycle)
            {
                if (TypeInfoOf(member) is { Kind: JsonTypeInfoKind.Object } typeInfo)
                {
                    _recursiveTypes.Add(typeInfo.Type);
                }
            }

            return;
        }

        ancestors.Add((pointer, obj));
        foreach (var property in obj)
        {
            // RFC 6901 escaping: "~" is "~0" and "/" is "~1".
            FindRecursiveTypes(property.Value, $"{pointer}/{property.Key.Replace("~", "~0").Replace("/", "~1")}", ancestors);
        }

        ancestors.RemoveAt(ancestors.Count - 1);
    }

    private AsyncApiJsonSchema Map(JsonNode node, HashSet<string> activeRefs)
    {
        if (node is JsonObject obj)
        {
            // A "$ref" is described by the schema it points to.
            var schemaNode = RefPointer(obj) is { } pointer ? ResolvePointer(pointer) as JsonObject : obj;
            if (TypeInfoOf(schemaNode) is { Kind: JsonTypeInfoKind.Object } typeInfo && _recursiveTypes.Contains(typeInfo.Type))
            {
                return Reference(typeInfo.Type, schemaNode, activeRefs);
            }
        }

        return MapSchema(node, activeRefs, componentSchema: false);
    }

    /// <summary>
    /// References the component schema of a recursive type, which is added from the first schema of
    /// the type encountered. The component describes the type itself, so a nullable occurrence
    /// references it and also allows null.
    /// </summary>
    private AsyncApiJsonSchema Reference(Type type, JsonObject schemaNode, HashSet<string> activeRefs)
    {
        var reference = new AsyncApiJsonSchemaReference(_addComponent(type, () => MapSchema(schemaNode, activeRefs, componentSchema: true)));

        return schemaNode["type"] is JsonArray types && types.Any((t) => (string)t == "null")
            ? new AsyncApiJsonSchema() { AnyOf = [reference, new AsyncApiJsonSchema() { Type = SchemaType.Null }] }
            : reference;
    }

    private AsyncApiJsonSchema MapSchema(JsonNode node, HashSet<string> activeRefs, bool componentSchema)
    {
        var schema = new AsyncApiJsonSchema();

        // JSON Schema allows boolean schemas: "true" accepts anything, "false" accepts nothing.
        if (node is JsonValue value && value.TryGetValue(out bool accepts))
        {
            if (!accepts)
            {
                schema.Not = new AsyncApiJsonSchema();
            }

            return schema;
        }

        if (node is not JsonObject obj)
        {
            return schema;
        }

        // A "$ref" that does not point at a recursive object type (for example one that points at a
        // collection of them) is inlined; what it points to references the recursive type in turn.
        // A cycle through no object type at all is broken by emitting an empty schema when a pointer
        // refers back into a subschema that is already being expanded.
        if (RefPointer(obj) is { } pointer)
        {
            if (!activeRefs.Add(pointer))
            {
                return schema;
            }

            try
            {
                var target = ResolvePointer(pointer);
                return target == null ? schema : Map(target, activeRefs);
            }
            finally
            {
                activeRefs.Remove(pointer);
            }
        }

        foreach (var property in obj)
        {
            switch (property.Key)
            {
                case "type":
                    schema.Type = MapType(property.Value);
                    if (componentSchema && schema.Type != SchemaType.Null)
                    {
                        schema.Type &= ~SchemaType.Null;
                    }

                    break;
                case "title":
                    schema.Title = (string)property.Value;
                    break;
                case "description":
                    schema.Description = (string)property.Value;
                    break;
                case "format":
                    schema.Format = (string)property.Value;
                    break;
                case "pattern":
                    schema.Pattern = (string)property.Value;
                    break;
                case "properties":
                    schema.Properties = ((JsonObject)property.Value).ToDictionary((p) => p.Key, (p) => Map(p.Value, activeRefs));
                    break;
                case "patternProperties":
                    schema.PatternProperties = ((JsonObject)property.Value).ToDictionary((p) => p.Key, (p) => Map(p.Value, activeRefs));
                    break;
                case "required":
                    var required = ((JsonArray)property.Value).Select((i) => (string)i).Where((name) => !IsOmittedWhenWriting(obj, name)).ToList();
                    if (required.Count > 0)
                    {
                        schema.Required = new HashSet<string>(required, StringComparer.Ordinal);
                    }

                    break;
                case "items":
                    schema.Items = Map(property.Value, activeRefs);
                    break;
                case "additionalProperties":
                    schema.AdditionalProperties = Map(property.Value, activeRefs);
                    break;
                case "enum":
                    schema.Enum = [.. ((JsonArray)property.Value).Select((i) => new AsyncApiAny(i?.DeepClone()))];
                    break;
                case "const":
                    schema.Const = new AsyncApiAny(property.Value?.DeepClone());
                    break;
                case "default":
                    schema.Default = new AsyncApiAny(property.Value?.DeepClone());
                    break;
                case "minimum":
                    schema.Minimum = (double)property.Value;
                    break;
                case "maximum":
                    schema.Maximum = (double)property.Value;
                    break;
                case "minLength":
                    schema.MinLength = (int)property.Value;
                    break;
                case "maxLength":
                    schema.MaxLength = (int)property.Value;
                    break;
                case "minItems":
                    schema.MinItems = (int)property.Value;
                    break;
                case "maxItems":
                    schema.MaxItems = (int)property.Value;
                    break;
                case "anyOf":
                    schema.AnyOf = [.. ((JsonArray)property.Value).Select((i) => Map(i, activeRefs))];
                    break;
                case "allOf":
                    schema.AllOf = [.. ((JsonArray)property.Value).Select((i) => Map(i, activeRefs))];
                    break;
                case "oneOf":
                    schema.OneOf = [.. ((JsonArray)property.Value).Select((i) => Map(i, activeRefs))];
                    break;
                case "not":
                    schema.Not = Map(property.Value, activeRefs);
                    break;
                default:
                    // Keywords the AsyncAPI model has no slot for (for example "$comment") are dropped.
                    break;
            }
        }

        return schema;
    }

    /// <summary>
    /// Whether the serializer can leave a property out of what it writes. The exporter lists
    /// constructor parameters and C# <c>required</c> members as required whatever their type, but
    /// options that ignore nulls (or defaults) when writing omit them whenever they are null (or
    /// default), so a producer's own messages would fail a schema that still requires them.
    /// </summary>
    private bool IsOmittedWhenWriting(JsonObject schema, string propertyName)
    {
        if (_ignoreCondition is not (JsonIgnoreCondition.WhenWritingNull or JsonIgnoreCondition.WhenWritingDefault)
            || schema["properties"] is not JsonObject properties
            || !properties.TryGetPropertyValue(propertyName, out var property))
        {
            return false;
        }

        if (AllowsNull(property))
        {
            return true;
        }

        return _ignoreCondition == JsonIgnoreCondition.WhenWritingDefault && TypeOf(property) is { IsValueType: true };
    }

    private bool AllowsNull(JsonNode node, int depth = 0)
    {
        if (node is JsonValue value && value.TryGetValue(out bool accepts))
        {
            return accepts;
        }

        if (node is not JsonObject obj || depth > 16)
        {
            return false;
        }

        if (RefPointer(obj) is { } pointer)
        {
            return ResolvePointer(pointer) is { } target && AllowsNull(target, depth + 1);
        }

        if (obj["type"] is { } type)
        {
            return type is JsonArray types
                ? types.Any((t) => (string)t == "null")
                : (string)type == "null";
        }

        if (obj["anyOf"] is JsonArray anyOf)
        {
            return anyOf.Any((branch) => AllowsNull(branch, depth + 1));
        }

        // A schema with no type constraint (for example an object-typed member) accepts null.
        return !obj.ContainsKey("enum") && !obj.ContainsKey("const") && !obj.ContainsKey("oneOf") && !obj.ContainsKey("allOf");
    }

    private Type TypeOf(JsonNode node) => TypeInfoOf(node)?.Type;

    private JsonTypeInfo TypeInfoOf(JsonNode node)
        => node is JsonObject obj && obj[TypeKeyword] is JsonValue index && index.TryGetValue(out int i) ? _types[i] : null;

    private static string RefPointer(JsonObject node)
        => node["$ref"] is JsonValue refValue && refValue.TryGetValue(out string pointer) ? pointer : null;

    private JsonNode ResolvePointer(string pointer)
    {
        if (pointer == "#")
        {
            return _root;
        }

        if (!pointer.StartsWith("#/", StringComparison.Ordinal))
        {
            // The exporter only produces local JSON Pointers; anything else is not resolvable here.
            return null;
        }

        JsonNode current = _root;
        foreach (var rawToken in pointer.Substring(2).Split('/'))
        {
            if (current == null)
            {
                return null;
            }

            // RFC 6901 escaping: "~1" is "/" and "~0" is "~".
            string token = rawToken.Replace("~1", "/").Replace("~0", "~");
            current = current switch
            {
                JsonObject o => o.TryGetPropertyValue(token, out var child) ? child : null,
                JsonArray a => int.TryParse(token, out int index) && index >= 0 && index < a.Count ? a[index] : null,
                _ => null,
            };
        }

        return current;
    }

    private static SchemaType MapType(JsonNode typeNode)
    {
        if (typeNode is JsonArray types)
        {
            SchemaType combined = 0;
            foreach (var type in types)
            {
                combined |= ParseType((string)type);
            }

            return combined;
        }

        return ParseType((string)typeNode);
    }

    private static SchemaType ParseType(string type) => type switch
    {
        "object" => SchemaType.Object,
        "array" => SchemaType.Array,
        "string" => SchemaType.String,
        "integer" => SchemaType.Integer,
        "number" => SchemaType.Number,
        "boolean" => SchemaType.Boolean,
        "null" => SchemaType.Null,
        _ => 0,
    };
}
