using CrudGenerator.Shared;

namespace CrudGenerator.Interfaces
{
    public interface ICodeGenerationHandler
    {
        Task<string> GenerateCodeAsync(string modelName, string projectName, List<(string Name, string Type)> attributes = null, List<Shared.Relationship> relationships = null);
    }

}
