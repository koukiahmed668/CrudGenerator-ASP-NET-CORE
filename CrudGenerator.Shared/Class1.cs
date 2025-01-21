using System.Text.Json;
using System.Text.Json.Serialization;

namespace CrudGenerator.Shared
{
    public class CodeGenerationRequest
    {
        public string ProjectName { get; set; } 
        public List<ModelDefinition> Models { get; set; }
        public string ResponseType { get; set; } // "zip" or "text"
        public List<string> Roles { get; set; }
        public bool IncludeJwtAuthentication { get; set; }


    }

    // Model definition for code generation
    public class ModelDefinition
    {
        public string Name { get; set; }
        public List<AttributeDefinition> Attributes { get; set; }
        public List<Relationship> Relationships { get; set; }

    }

    // Attribute definition
    public class AttributeDefinition
    {
        public string Name { get; set; }
        public string Type { get; set; }
    }

    public class Relationship
    {
        public string PropertyName { get; set; }  // e.g., "Orders" in Customer model for one-to-many relationship
        public string TargetModel { get; set; }    // e.g., "Order" for one-to-many relationship

        [JsonConverter(typeof(RelationshipTypeConverter))]
        public RelationshipType Type { get; set; }
    }

    public enum RelationshipType
    {
        OneToMany,
        ManyToOne,
        ManyToMany
    }


    public class RelationshipTypeConverter : JsonConverter<RelationshipType>
    {
        public override RelationshipType Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
        {
            if (reader.TokenType == JsonTokenType.String)
            {
                string value = reader.GetString();
                return value switch
                {
                    "OneToMany" => RelationshipType.OneToMany,
                    "ManyToOne" => RelationshipType.ManyToOne,
                    "ManyToMany" => RelationshipType.ManyToMany,
                    _ => throw new JsonException($"Invalid value for RelationshipType: {value}")
                };
            }

            if (reader.TokenType == JsonTokenType.Number)
            {
                int value = reader.GetInt32();
                return value switch
                {
                    1 => RelationshipType.OneToMany,
                    2 => RelationshipType.ManyToOne,
                    3 => RelationshipType.ManyToMany,
                    _ => throw new JsonException($"Invalid value for RelationshipType: {value}")
                };
            }

            throw new JsonException($"Unexpected token {reader.TokenType} when reading RelationshipType.");
        }

        public override void Write(Utf8JsonWriter writer, RelationshipType value, JsonSerializerOptions options)
        {
            string enumValue = value switch
            {
                RelationshipType.OneToMany => "OneToMany",
                RelationshipType.ManyToOne => "ManyToOne",
                RelationshipType.ManyToMany => "ManyToMany",
                _ => throw new JsonException($"Invalid value for RelationshipType: {value}")
            };
            writer.WriteStringValue(enumValue);
        }
    }
}
