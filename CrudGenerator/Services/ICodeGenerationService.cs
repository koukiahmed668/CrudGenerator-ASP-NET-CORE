using CrudGenerator.Controllers;

namespace CrudGenerator.Services
{
    public interface ICodeGenerationService
    {
        Task<string> GenerateModelCode(string modelName, List<(string Name, string Type)> attributes, List<Relationship> relationships, string projectName);
        Task<string> GenerateServiceCode(string modelName, string projectName);
        Task<string> GenerateControllerCode(string modelName, string projectName);
        Task<string> GenerateRepositoryCode(string modelName, string projectName);
        Task<string> GenerateDbContextCode(List<ModelDefinition> models, bool includeJwtAuthentication, string projectName);

        // New methods for authentication and authorization code generation
        Task<string> GenerateJwtAuthenticationManagerCode( string projectName);
        Task<string> GenerateJwtAuthenticationControllerCode(string projectName);
        Task<string> GenerateUserServiceCode( string projectName);
        Task<string> GenerateUserRepositoryCode( string projectName);
        Task<string> GenerateUserEntityCode( string projectName);
        Task<string> GenerateJwtMiddlewareCode( string projectName);
        Task<string> GenerateAuthorizationCode(List<string> roles, string projectName);

        // New methods for full application generation
        Task<string> GenerateProgramCs(List<string> modelNames, bool includeJwtAuthentication, string projectName);
        Task<string> GenerateProjectFile(string projectName);
        Task<string> GenerateAppSettingsJson();
    }
}
