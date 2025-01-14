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
            var generatedFiles = new Dictionary<string, string>();

            // Log usage info before code generation
            var usageLog = new UsageLog
            {
                UserIp = HttpContext.Connection.RemoteIpAddress?.ToString(),
                Timestamp = DateTime.UtcNow,
                GeneratedModels = request.Models.Select(m => m.Name).ToList(),
                ResponseType = request.ResponseType,
                Roles = request.Roles,
                JwtIncluded = request.IncludeJwtAuthentication
            };

            await _usageLogService.LogUsageAsync(usageLog);

            foreach (var model in request.Models)
            {
                // Convert attributes to the expected format (List<(string Name, string Type)>)
                var attributes = model.Attributes
                    .Select(attr => (attr.Name, attr.Type))
                    .ToList();

                // Generate code for the model
                var modelCode = await _codeGenerationService.GenerateModelCode(model.Name, attributes, model.Relationships);
                generatedFiles.Add($"Models/{model.Name}.cs", modelCode); // Add to Models folder

                // Generate service
                var serviceCode = await _codeGenerationService.GenerateServiceCode(model.Name);
                generatedFiles.Add($"Services/I{model.Name}Service.cs", serviceCode); // Add to Services folder

                // Generate repository
                var repositoryCode = await _codeGenerationService.GenerateRepositoryCode(model.Name);
                generatedFiles.Add($"Repositories/I{model.Name}Repository.cs", repositoryCode); // Add to Repositories folder

                // Generate controller
                var controllerCode = await _codeGenerationService.GenerateControllerCode(model.Name);
                generatedFiles.Add($"Controllers/{model.Name}Controller.cs", controllerCode); // Add to Controllers folder
            }


            // Generate DbContext

            var dbContextCode = await _codeGenerationService.GenerateDbContextCode(request.Models, request.IncludeJwtAuthentication);
            generatedFiles.Add("AppDbContext.cs", dbContextCode);

            // Conditionally generate JWT-related files
            if (request.IncludeJwtAuthentication)
            {

                // Generate JWT Authentication Manager
                var jwtAuthManagerCode = await _codeGenerationService.GenerateJwtAuthenticationManagerCode();
                generatedFiles.Add($"Authentication/JwtAuthenticationManager.cs", jwtAuthManagerCode);

                // Generate JWT Authentication Controller
                var jwtAuthControllerCode = await _codeGenerationService.GenerateJwtAuthenticationControllerCode();
                generatedFiles.Add($"Authentication/JwtAuthenticationController.cs", jwtAuthControllerCode);

                // Generate JWT Middleware
                var jwtMiddlewareCode = await _codeGenerationService.GenerateJwtMiddlewareCode();
                generatedFiles.Add($"Authentication/JwtMiddleware.cs", jwtMiddlewareCode);

                // Generate Authorization Code
                var roles = request.Roles ?? new List<string> { "User" };  // Default role is User
                var authorizationCode = await _codeGenerationService.GenerateAuthorizationCode(roles);
                generatedFiles.Add($"Authentication/Extensions/AuthorizationExtensions.cs", authorizationCode);

                // Generate User Service
                var userServiceCode = await _codeGenerationService.GenerateUserServiceCode();
                generatedFiles.Add($"Services/UserService.cs", userServiceCode);

                // Generate User Repository
                var userRepositoryCode = await _codeGenerationService.GenerateUserRepositoryCode();
                generatedFiles.Add($"Repositories/UserRepository.cs", userRepositoryCode);

                // Generate User Entity
                var userEntityCode = await _codeGenerationService.GenerateUserEntityCode();
                generatedFiles.Add($"Models/User.cs", userEntityCode);

            }
            // Generate Program.cs
            var programCsCode = await _codeGenerationService.GenerateProgramCs(
                request.Models.ConvertAll(m => m.Name),
                request.IncludeJwtAuthentication
            );
            generatedFiles.Add("Program.cs", programCsCode);


            // Generate project file
            var projectFileCode = await _codeGenerationService.GenerateProjectFile("GeneratedApp");
            generatedFiles.Add("GeneratedApp.csproj", projectFileCode);

            // Generate appsettings.json
            var appSettingsJsonCode = await _codeGenerationService.GenerateAppSettingsJson();
            generatedFiles.Add("appsettings.json", appSettingsJsonCode);


            // Check the response type
            if (request.ResponseType == "zip")
            {
                var zipStream = CreateZipStream(generatedFiles);
                return File(zipStream, "application/zip", "GeneratedCode.zip");
            }

            // Return as plain text (e.g., JSON)
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
