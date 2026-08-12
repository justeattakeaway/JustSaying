using System.Text.Json.Nodes;
using ByteBard.AsyncAPI.Models;

namespace JustSaying.AsyncApi;

/// <summary>
/// Maps the JSON Schema produced by <see cref="System.Text.Json.Schema.JsonSchemaExporter"/>
/// onto the AsyncAPI schema object model.
/// </summary>
internal static class JsonSchemaNodeMapper
{
    public static AsyncApiJsonSchema Map(JsonNode node)
        => Map(node, node, new HashSet<string>(StringComparer.Ordinal));

    private static AsyncApiJsonSchema Map(JsonNode node, JsonNode root, HashSet<string> activeRefs)
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
                var target = ResolvePointer(root, pointer);
                return target == null ? schema : Map(target, root, activeRefs);
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
                    schema.Properties = ((JsonObject)property.Value).ToDictionary((p) => p.Key, (p) => Map(p.Value, root, activeRefs));
                    break;
                case "patternProperties":
                    schema.PatternProperties = ((JsonObject)property.Value).ToDictionary((p) => p.Key, (p) => Map(p.Value, root, activeRefs));
                    break;
                case "required":
                    schema.Required = new HashSet<string>(((JsonArray)property.Value).Select((i) => (string)i), StringComparer.Ordinal);
                    break;
                case "items":
                    schema.Items = Map(property.Value, root, activeRefs);
                    break;
                case "additionalProperties":
                    schema.AdditionalProperties = Map(property.Value, root, activeRefs);
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
                    schema.AnyOf = [.. ((JsonArray)property.Value).Select((i) => Map(i, root, activeRefs))];
                    break;
                case "allOf":
                    schema.AllOf = [.. ((JsonArray)property.Value).Select((i) => Map(i, root, activeRefs))];
                    break;
                case "oneOf":
                    schema.OneOf = [.. ((JsonArray)property.Value).Select((i) => Map(i, root, activeRefs))];
                    break;
                case "not":
                    schema.Not = Map(property.Value, root, activeRefs);
                    break;
                default:
                    // Keywords the AsyncAPI model has no slot for (for example "$comment") are dropped.
                    break;
            }
        }

        return schema;
    }

    private static JsonNode ResolvePointer(JsonNode root, string pointer)
    {
        if (pointer == "#")
        {
            return root;
        }

        if (!pointer.StartsWith("#/", StringComparison.Ordinal))
        {
            // The exporter only produces local JSON Pointers; anything else is not resolvable here.
            return null;
        }

        JsonNode current = root;
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
