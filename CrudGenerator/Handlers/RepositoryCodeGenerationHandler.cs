using CrudGenerator.Interfaces;
using CrudGenerator.Shared;
namespace CrudGenerator.Handlers
{
    public class RepositoryCodeGenerationHandler : ICodeGenerationHandler
    {
        private readonly string _templateDirectory = Path.Combine(Directory.GetCurrentDirectory(), "Templates");

        public async Task<string> GenerateCodeAsync(string modelName, string projectName, List<(string Name, string Type)> attributes = null, List<Shared.Relationship> relationships = null)
        {
            var template = await ReadTemplateAsync("RepositoryTemplate.txt");
            return template.Replace("{{ModelName}}", modelName)
                           .Replace("{{ModelNamePlural}}", Pluralize(modelName))
                           .Replace("{{ProjectName}}", projectName);
        }

        private async Task<string> ReadTemplateAsync(string fileName)
        {
            var filePath = Path.Combine(_templateDirectory, fileName);
            if (!File.Exists(filePath))
                throw new FileNotFoundException($"Template file '{fileName}' not found in '{_templateDirectory}'.");

            return await File.ReadAllTextAsync(filePath);
        }

        private string Pluralize(string name)
        {
            if (name.EndsWith("y"))
                return name.Substring(0, name.Length - 1) + "ies";

            return name + "s";
        }
    }

}
