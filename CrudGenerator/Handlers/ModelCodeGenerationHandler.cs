using CrudGenerator.Interfaces;
using CrudGenerator.Shared;

namespace CrudGenerator.Handlers
{
    public class ModelCodeGenerationHandler : ICodeGenerationHandler
    {
        private readonly string _templateDirectory = Path.Combine(Directory.GetCurrentDirectory(), "Templates");

        public async Task<string> GenerateCodeAsync(string modelName, string projectName, List<(string Name, string Type)> attributes, List<Shared.Relationship> relationships)
        {
            var template = await ReadTemplateAsync("ModelTemplate.txt");

            var attributeDefinitions = string.Join(Environment.NewLine,
                attributes.Select(attr => $"        public {attr.Type} {attr.Name} {{ get; set; }}"));

            var foreignKeyAttributes = string.Join(Environment.NewLine,
                relationships.Where(r => r.Type == RelationshipType.ManyToOne)
                              .Select(rel => $"        public int {rel.TargetModel}Id {{ get; set; }}"));

            attributeDefinitions = string.Join(Environment.NewLine, new[] { attributeDefinitions, foreignKeyAttributes });

            var relationshipDefinitions = string.Join(Environment.NewLine,
                relationships.Select(rel => GenerateRelationshipCode(rel)));

            return template.Replace("{{ModelName}}", modelName)
                           .Replace("{{Attributes}}", attributeDefinitions)
                           .Replace("{{Relationships}}", relationshipDefinitions)
                           .Replace("{{ProjectName}}", projectName);
        }

        private string GenerateRelationshipCode(Relationship relationship)
        {
            switch (relationship.Type)
            {
                case RelationshipType.OneToMany:
                    return $"        public ICollection<{relationship.TargetModel}> {relationship.PropertyName} {{ get; set; }}";
                case RelationshipType.ManyToOne:
                    return $"        public {relationship.TargetModel} {relationship.PropertyName} {{ get; set; }}";
                case RelationshipType.ManyToMany:
                    return $"        public ICollection<{relationship.TargetModel}> {relationship.PropertyName} {{ get; set; }}";
                default:
                    return string.Empty;
            }
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
