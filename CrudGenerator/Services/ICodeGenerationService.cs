using CrudGenerator.Controllers;

namespace CrudGenerator.Services
{
    public interface ICodeGenerationService
    {
        Task<string> GenerateModelCode(string modelName, List<(string Name, string Type)> attributes, List<Relationship> relationships);
        Task<string> GenerateServiceCode(string modelName);
        Task<string> GenerateControllerCode(string modelName);
        Task<string> GenerateRepositoryCode(string modelName);
        Task<string> GenerateDbContextCode(List<ModelDefinition> models);

        // New methods for authentication and authorization code generation
        Task<string> GenerateJwtAuthenticationManagerCode();
        Task<string> GenerateJwtMiddlewareCode();
        Task<string> GenerateAuthorizationCode(List<string> roles);

        // New methods for full application generation
        Task<string> GenerateProgramCs(List<string> modelNames);
        Task<string> GenerateProjectFile(string projectName);
        Task<string> GenerateAppSettingsJson();
    }
}
