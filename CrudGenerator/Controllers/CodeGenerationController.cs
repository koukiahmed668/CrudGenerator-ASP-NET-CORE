using CrudGenerator.Services;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using System.Collections.Generic;
using System.IO;
using System.IO.Compression;
using System.Threading.Tasks;

namespace CrudGenerator.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class CodeGenerationController : ControllerBase
    {
        private readonly ICodeGenerationService _codeGenerationService;

        public CodeGenerationController(ICodeGenerationService codeGenerationService)
        {
            _codeGenerationService = codeGenerationService;
        }

        // Endpoint to generate code
        [HttpPost("generate")]
        public async Task<IActionResult> GenerateCode([FromBody] CodeGenerationRequest request)
        {
            var generatedFiles = new Dictionary<string, string>();

            // Collect model definitions with relationships
            var modelsWithRelationships = new List<(string ModelName, List<(string RelatedModel, string RelationshipType)> Relationships)>();

            foreach (var model in request.Models)
            {
                // Convert attributes to the expected format (List<(string Name, string Type)>)
                var attributes = model.Attributes
                    .Select(attr => (attr.Name, attr.Type))
                    .ToList();

                // You should pass relationships too (model.Relationships)
                var relationships = model.Relationships;

                // Generate model code
                var modelCode = await _codeGenerationService.GenerateModelCode(model.Name, attributes, relationships);
                generatedFiles.Add($"{model.Name}.cs", modelCode);

                // Generate service
                var serviceCode = await _codeGenerationService.GenerateServiceCode(model.Name);
                generatedFiles.Add($"I{model.Name}Service.cs", serviceCode);

                // Generate repository
                var repositoryCode = await _codeGenerationService.GenerateRepositoryCode(model.Name);
                generatedFiles.Add($"I{model.Name}Repository.cs", repositoryCode);

                // Generate controller
                var controllerCode = await _codeGenerationService.GenerateControllerCode(model.Name);
                generatedFiles.Add($"{model.Name}Controller.cs", controllerCode);

                // Add this model and its relationships for DbContext generation
                modelsWithRelationships.Add((model.Name, relationships));
            }

            // Generate DbContext code with model relationships
            var dbContextCode = await _codeGenerationService.GenerateDbContextCode(modelsWithRelationships);
            generatedFiles.Add("AppDbContext.cs", dbContextCode);

            // Generate JWT Authentication Manager
            var jwtAuthManagerCode = await _codeGenerationService.GenerateJwtAuthenticationManagerCode();
            generatedFiles.Add("JwtAuthenticationManager.cs", jwtAuthManagerCode);

            // Generate JWT Middleware
            var jwtMiddlewareCode = await _codeGenerationService.GenerateJwtMiddlewareCode();
            generatedFiles.Add("JwtMiddleware.cs", jwtMiddlewareCode);

            // Generate Authorization Code
            var roles = request.Roles ?? new List<string> { "User" };  // Default role is User
            var authorizationCode = await _codeGenerationService.GenerateAuthorizationCode(roles);
            generatedFiles.Add("AuthorizationExtensions.cs", authorizationCode);

            // Generate Program.cs
            var programCsCode = await _codeGenerationService.GenerateProgramCs(request.Models.ConvertAll(m => m.Name));
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
                    var zipEntry = archive.CreateEntry(file.Key);
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
        public List<string> Roles { get; set; } // Add this property for roles
    }

    // Model definition for code generation
    public class ModelDefinition
    {
        public string Name { get; set; }
        public List<AttributeDefinition> Attributes { get; set; }
        public List<(string RelatedModel, string RelationshipType)> Relationships { get; set; } // Add relationships here
    }

    // Attribute definition
    public class AttributeDefinition
    {
        public string Name { get; set; }
        public string Type { get; set; }
    }
}
