using CrudGenerator.Data;
using CrudGenerator.Models;
using CrudGenerator.Services;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using System.Collections.Generic;
using System.IO;
using System.IO.Compression;
using System.Text.Json;
using System.Text.Json.Serialization;
using System.Threading.Tasks;

namespace CrudGenerator.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class CodeGenerationController : ControllerBase
    {
        private readonly ICodeGenerationService _codeGenerationService;
        private readonly IUsageLogService _usageLogService;
        private readonly AppDbContext _context;

        public CodeGenerationController(ICodeGenerationService codeGenerationService, IUsageLogService usageLogService, AppDbContext context)
        {
            _codeGenerationService = codeGenerationService;
            _usageLogService = usageLogService;
            _context = context;
        }

        // Endpoint to generate code
        [HttpPost("generate")]
        public async Task<IActionResult> GenerateCode([FromBody] CodeGenerationRequest request)
        {
            if (string.IsNullOrWhiteSpace(request.ProjectName))
                return BadRequest("ProjectName is required.");

            var generatedFiles = new Dictionary<string, string>();

            foreach (var model in request.Models)
            {
                var attributes = model.Attributes.Select(attr => (attr.Name, attr.Type)).ToList();

                // Generate code for the model
                var modelCode = await _codeGenerationService.GenerateModelCode(model.Name, attributes, model.Relationships, request.ProjectName);
                generatedFiles.Add($"Models/{model.Name}.cs", modelCode);

                // Generate service
                var serviceCode = await _codeGenerationService.GenerateServiceCode(model.Name, request.ProjectName);
                generatedFiles.Add($"Services/I{model.Name}Service.cs", serviceCode);

                // Generate repository
                var repositoryCode = await _codeGenerationService.GenerateRepositoryCode(model.Name, request.ProjectName);
                generatedFiles.Add($"Repositories/I{model.Name}Repository.cs", repositoryCode);

                // Generate controller
                var controllerCode = await _codeGenerationService.GenerateControllerCode(model.Name, request.ProjectName);
                generatedFiles.Add($"Controllers/{model.Name}Controller.cs", controllerCode);
            }

            // Generate DbContext
            var dbContextCode = await _codeGenerationService.GenerateDbContextCode(request.Models, request.IncludeJwtAuthentication, request.ProjectName);
            generatedFiles.Add("AppDbContext.cs", dbContextCode);

            if (request.IncludeJwtAuthentication)
            {
                var jwtAuthManagerCode = await _codeGenerationService.GenerateJwtAuthenticationManagerCode(request.ProjectName);
                generatedFiles.Add($"Authentication/JwtAuthenticationManager.cs", jwtAuthManagerCode);

                var jwtAuthControllerCode = await _codeGenerationService.GenerateJwtAuthenticationControllerCode(request.ProjectName);
                generatedFiles.Add($"Authentication/JwtAuthenticationController.cs", jwtAuthControllerCode);

                var jwtMiddlewareCode = await _codeGenerationService.GenerateJwtMiddlewareCode(request.ProjectName);
                generatedFiles.Add($"Authentication/JwtMiddleware.cs", jwtMiddlewareCode);

                var roles = request.Roles ?? new List<string> { "User" };
                var authorizationCode = await _codeGenerationService.GenerateAuthorizationCode(roles, request.ProjectName);
                generatedFiles.Add($"Authentication/Extensions/AuthorizationExtensions.cs", authorizationCode);

                var userServiceCode = await _codeGenerationService.GenerateUserServiceCode(request.ProjectName);
                generatedFiles.Add($"Services/UserService.cs", userServiceCode);

                var userRepositoryCode = await _codeGenerationService.GenerateUserRepositoryCode(request.ProjectName);
                generatedFiles.Add($"Repositories/UserRepository.cs", userRepositoryCode);

                var userEntityCode = await _codeGenerationService.GenerateUserEntityCode(request.ProjectName);
                generatedFiles.Add($"Models/User.cs", userEntityCode);
            }

            // Generate Program.cs
            var programCsCode = await _codeGenerationService.GenerateProgramCs(
                request.Models.ConvertAll(m => m.Name),
                request.IncludeJwtAuthentication,
                request.ProjectName
            );
            generatedFiles.Add("Program.cs", programCsCode);

            // Generate project file
            var projectFileCode = await _codeGenerationService.GenerateProjectFile(request.ProjectName);
            generatedFiles.Add($"{request.ProjectName}.csproj", projectFileCode);

            // Generate appsettings.json
            var appSettingsJsonCode = await _codeGenerationService.GenerateAppSettingsJson();
            generatedFiles.Add("appsettings.json", appSettingsJsonCode);

            // Check the response type
            if (request.ResponseType == "zip")
            {
                var zipStream = CreateZipStream(generatedFiles);
                return File(zipStream, "application/zip", "GeneratedCode.zip");
            }

            return Ok(generatedFiles);
        }

        // Create a zip stream from the generated files
        private MemoryStream CreateZipStream(Dictionary<string, string> files)
        {
            var zipStream = new MemoryStream();
            using (var archive = new ZipArchive(zipStream, ZipArchiveMode.Create, true))
            {
                foreach (var file in files)
                {
                    var zipEntry = archive.CreateEntry(file.Key); // file.Key includes the folder path
                    using (var entryStream = zipEntry.Open())
                    using (var streamWriter = new StreamWriter(entryStream))
                    {
                        streamWriter.Write(file.Value);
                    }
                }
            }
            zipStream.Seek(0, SeekOrigin.Begin);
            return zipStream;
        }
    }

    // Request model for code generation
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
