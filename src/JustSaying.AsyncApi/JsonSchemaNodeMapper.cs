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
                    schema.Properties = ((JsonObject)property.Value).ToDictionary((p) => p.Key, (p) => Map(p.Value));
                    break;
                case "patternProperties":
                    schema.PatternProperties = ((JsonObject)property.Value).ToDictionary((p) => p.Key, (p) => Map(p.Value));
                    break;
                case "required":
                    schema.Required = new HashSet<string>(((JsonArray)property.Value).Select((i) => (string)i), StringComparer.Ordinal);
                    break;
                case "items":
                    schema.Items = Map(property.Value);
                    break;
                case "additionalProperties":
                    schema.AdditionalProperties = Map(property.Value);
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
                    schema.AnyOf = [.. ((JsonArray)property.Value).Select(Map)];
                    break;
                case "allOf":
                    schema.AllOf = [.. ((JsonArray)property.Value).Select(Map)];
                    break;
                case "oneOf":
                    schema.OneOf = [.. ((JsonArray)property.Value).Select(Map)];
                    break;
                case "not":
                    schema.Not = Map(property.Value);
                    break;
                default:
                    // Keywords the AsyncAPI model has no slot for (for example "$comment") are dropped.
                    break;
            }
        }

        return schema;
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
