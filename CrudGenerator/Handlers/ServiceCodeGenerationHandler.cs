using CrudGenerator.Interfaces;
using CrudGenerator.Shared;

namespace CrudGenerator.Handlers
{
    public class ServiceCodeGenerationHandler : ICodeGenerationHandler
    {
        private readonly string _templateDirectory = Path.Combine(Directory.GetCurrentDirectory(), "Templates");

        public async Task<string> GenerateCodeAsync(string modelName, string projectName, List<(string Name, string Type)> attributes = null, List<Relationship> relationships = null)
        {
            var template = await ReadTemplateAsync("ServiceTemplate.txt");
            return template.Replace("{{ModelName}}", modelName)
                           .Replace("{{ProjectName}}", projectName);
        }

        private async Task<string> ReadTemplateAsync(string fileName)
        {
            var filePath = Path.Combine(_templateDirectory, fileName);
            if (!File.Exists(filePath))
                throw new FileNotFoundException($"Template file '{fileName}' not found in '{_templateDirectory}'.");

            return await File.ReadAllTextAsync(filePath);
        }
    }
}
