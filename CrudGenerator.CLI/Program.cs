using System;
using System.Collections.Generic;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Text;
using System.Threading.Tasks;
using System.Text.Json;
using System.IO.Compression;
using System.IO;
using System.Text.Json.Serialization;

class Program
{
    static async Task<int> Main(string[] args)
    {
        Console.WriteLine("Welcome to the CRUD Generator CLI");

        // Start the interactive prompt for generating CRUD code
        var models = new List<ModelDefinition>();
        bool done = false;

        while (!done)
        {
            Console.WriteLine("Do you want to add a model? (yes/no):");
            var addModelResponse = Console.ReadLine()?.Trim().ToLower();

            if (addModelResponse == "yes")
            {
                var model = await PromptForModel();
                models.Add(model);

                Console.WriteLine("Are you done with this model? (yes/no):");
                var doneResponse = Console.ReadLine()?.Trim().ToLower();
                if (doneResponse == "yes")
                {
                    done = true;
                }
            }
            else
            {
                done = true;
            }
        }

        // Ask about relationships and other configurations
        var relationships = await PromptForRelationships();
        var includeJwt = await PromptForJwt();
        var includeRoles = await PromptForRoles();

        // Create the CodeGenerationRequest
        var request = new CodeGenerationRequest
        {
            Models = models,
            ResponseType = "zip", // or "text", depending on your use case
            Roles = includeRoles ? new List<string> { "Admin", "User" } : new List<string>(),
            IncludeJwtAuthentication = includeJwt
        };

        // Print out the summary for testing
        Console.WriteLine("Code generation request prepared: ");
        Console.WriteLine($"Models: {string.Join(", ", models.Select(m => m.Name))}");
        Console.WriteLine($"Include JWT: {includeJwt}");
        Console.WriteLine($"Include Roles: {includeRoles}");

        // Send request to the API to generate the ZIP file
        var apiUrl = "https://crudgenerator-asp-net-core.onrender.com/api/CodeGeneration/generate"; // Replace with your actual API endpoint
        await SendRequestToApi(apiUrl, request);

        return 0;
    }

    static async Task<ModelDefinition> PromptForModel()
    {
        Console.WriteLine("Enter model name:");
        var modelName = Console.ReadLine()?.Trim();

        var model = new ModelDefinition { Name = modelName, Attributes = new List<AttributeDefinition>(), Relationships = new List<Relationship>() };

        // Ask for attributes
        var attributes = new List<AttributeDefinition>();
        bool done = false;

        while (!done)
        {
            Console.WriteLine("Enter attribute name (or type 'done' to finish):");
            var attributeName = Console.ReadLine()?.Trim();

            if (attributeName?.ToLower() == "done")
            {
                done = true;
            }
            else
            {
                // Ask for attribute type with built-in options
                var attributeType = await PromptForAttributeType();
                attributes.Add(new AttributeDefinition { Name = attributeName, Type = attributeType });
            }
        }

        model.Attributes = attributes;
        return model;
    }

    static async Task<string> PromptForAttributeType()
    {
        // Predefined built-in attribute types
        var builtInTypes = new List<string> { "int", "string", "DateTime", "bool", "decimal", "double", "Guid" };

        Console.WriteLine("Select an attribute type:");
        for (int i = 0; i < builtInTypes.Count; i++)
        {
            Console.WriteLine($"{i + 1}. {builtInTypes[i]}");
        }

        Console.WriteLine($"{builtInTypes.Count + 1}. Custom Type");

        var selection = Console.ReadLine()?.Trim();

        if (int.TryParse(selection, out var index) && index > 0 && index <= builtInTypes.Count)
        {
            return builtInTypes[index - 1]; // Return selected built-in type
        }
        else if (index == builtInTypes.Count + 1)
        {
            Console.WriteLine("Enter custom attribute type:");
            return Console.ReadLine()?.Trim();
        }
        else
        {
            Console.WriteLine("Invalid selection, defaulting to 'string'");
            return "string";
        }
    }

    static async Task<List<Relationship>> PromptForRelationships()
    {
        var relationships = new List<Relationship>();
        bool done = false;

        while (!done)
        {
            Console.WriteLine("Do you want to add a relationship? (yes/no):");
            var addRelationshipResponse = Console.ReadLine()?.Trim().ToLower();

            if (addRelationshipResponse == "yes")
            {
                var relationship = await PromptForRelationship();
                relationships.Add(relationship);
            }
            else
            {
                done = true;
            }
        }

        return relationships;
    }

    static async Task<Relationship> PromptForRelationship()
    {
        Console.WriteLine("Enter the source model for the relationship:");
        var sourceModel = Console.ReadLine()?.Trim();

        Console.WriteLine("Enter the target model for the relationship:");
        var targetModel = Console.ReadLine()?.Trim();

        Console.WriteLine("Enter the relationship type (OneToMany, ManyToOne, ManyToMany):");
        var relationshipType = Enum.Parse<RelationshipType>(Console.ReadLine()?.Trim(), true);

        return new Relationship
        {
            PropertyName = sourceModel + "s",  // Default pluralization (can be customized)
            TargetModel = targetModel,
            Type = relationshipType
        };
    }

    static async Task<bool> PromptForJwt()
    {
        Console.WriteLine("Do you want to include JWT authentication? (yes/no):");
        var includeJwtResponse = Console.ReadLine()?.Trim().ToLower();
        return includeJwtResponse == "yes";
    }

    static async Task<bool> PromptForRoles()
    {
        Console.WriteLine("Do you want to include roles-based authentication? (yes/no):");
        var includeRolesResponse = Console.ReadLine()?.Trim().ToLower();
        return includeRolesResponse == "yes";
    }

    static async Task SendRequestToApi(string apiUrl, CodeGenerationRequest request)
    {
        using (var client = new HttpClient())
        {
            try
            {
                // Serialize the request object to JSON
                var content = new StringContent(JsonSerializer.Serialize(request), Encoding.UTF8, "application/json");

                // Send the POST request
                var response = await client.PostAsync(apiUrl, content);

                // Check for a successful response
                if (response.IsSuccessStatusCode)
                {
                    Console.WriteLine("Code generation request successful. Retrieving ZIP file...");

                    // Assuming the API returns the ZIP file as a response
                    var zipFile = await response.Content.ReadAsByteArrayAsync();

                    // Get the current working directory (the directory the user is running the CLI from)
                    string currentDirectory = Directory.GetCurrentDirectory();

                    // Define the full file path where the ZIP file will be saved
                    var zipFilePath = Path.Combine(currentDirectory, "generated_code.zip");

                    // Save the ZIP file in the current directory
                    await System.IO.File.WriteAllBytesAsync(zipFilePath, zipFile);
                    Console.WriteLine($"ZIP file saved as {zipFilePath}");

                    // Extract the ZIP file in the current directory
                    string extractPath = Path.Combine(currentDirectory, "generated_code");

                    if (!Directory.Exists(extractPath))
                    {
                        Directory.CreateDirectory(extractPath);
                    }

                    // Extract the ZIP file
                    ZipFile.ExtractToDirectory(zipFilePath, extractPath);
                    Console.WriteLine($"ZIP file extracted to {extractPath}");

                    // Optionally, delete the ZIP file after extraction
                    File.Delete(zipFilePath);
                    Console.WriteLine("ZIP file deleted after extraction.");
                }
                else
                {
                    Console.WriteLine($"Failed to generate code. API responded with status: {response.StatusCode}");
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"An error occurred while sending the request: {ex.Message}");
            }
        }
    }


    public class CodeGenerationRequest
    {
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
