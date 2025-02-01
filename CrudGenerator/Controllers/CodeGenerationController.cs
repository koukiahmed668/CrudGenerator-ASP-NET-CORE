using CrudGenerator.Data;
using CrudGenerator.Interfaces;
using CrudGenerator.Shared;
using CrudGenerator.Models;
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

        public CodeGenerationController(ICodeGenerationService codeGenerationService)
        {
            _codeGenerationService = codeGenerationService;
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

                // Generate code for the model using handlers
                var modelCode = await _codeGenerationService.GenerateModelCode(model.Name, attributes, model.Relationships, request.ProjectName);
                generatedFiles.Add($"Models/{model.Name}.cs", modelCode);

                // Generate service using handlers
                var serviceCode = await _codeGenerationService.GenerateServiceCode(model.Name, request.ProjectName);
                generatedFiles.Add($"Services/I{model.Name}Service.cs", serviceCode);

                // Generate repository using handlers
                var repositoryCode = await _codeGenerationService.GenerateRepositoryCode(model.Name, request.ProjectName);
                generatedFiles.Add($"Repositories/I{model.Name}Repository.cs", repositoryCode);

                // Generate controller using handlers
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

}
