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
internal sealed class JsonSchemaNodeMapper
{
    // Marks each exported schema node with the index of the type it describes in _types. Unknown
    // keywords are never mapped, so the marker does not reach the document.
    private const string TypeKeyword = "$justSayingType";

    private readonly List<JsonTypeInfo> _types = [];
    private readonly JsonIgnoreCondition _ignoreCondition;
    private JsonNode _root;

    /// <summary>
    /// Initializes a new instance of the <see cref="JsonSchemaNodeMapper"/> class.
    /// </summary>
    /// <param name="serializerOptions">The options the payload is serialized with, which decide what is written.</param>
    public JsonSchemaNodeMapper(JsonSerializerOptions serializerOptions)
    {
        _ignoreCondition = serializerOptions.DefaultIgnoreCondition;
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

    private AsyncApiJsonSchema Map(JsonNode node, HashSet<string> activeRefs)
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

        // The schema exporter emits "$ref" as a JSON Pointer into the schema itself for recursive
        // types (for example "#/properties/Left"). Those pointers are relative to the exported
        // schema's root and would not resolve once embedded in the wider AsyncAPI document, so the
        // referenced subschema is inlined instead. Cycles are broken by emitting an empty schema
        // when a pointer refers back into a subschema that is already being expanded.
        if (obj.TryGetPropertyValue("$ref", out var refNode)
            && refNode is JsonValue refValue
            && refValue.TryGetValue(out string pointer))
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

        if (obj["$ref"] is JsonValue refValue && refValue.TryGetValue(out string pointer))
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

    private Type TypeOf(JsonNode node)
        => node is JsonObject obj && obj[TypeKeyword] is JsonValue index && index.TryGetValue(out int i) ? _types[i].Type : null;

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
