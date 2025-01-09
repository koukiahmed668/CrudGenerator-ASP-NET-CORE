using CrudGenerator.Controllers;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;

namespace CrudGenerator.Services
{
    public class CodeGenerationService : ICodeGenerationService
    {
        private readonly string _templateDirectory = Path.Combine(Directory.GetCurrentDirectory(), "Templates");

        public async Task<string> GenerateModelCode(string modelName, List<(string Name, string Type)> attributes)
        {
            var template = await ReadTemplateAsync("ModelTemplate.txt");

            var attributeDefinitions = string.Join(Environment.NewLine,
                attributes.Select(attr => $"        public {attr.Type} {attr.Name} {{ get; set; }}"));

            return template.Replace("{{ModelName}}", modelName)
                           .Replace("{{Attributes}}", attributeDefinitions);
        }

        public async Task<string> GenerateServiceCode(string modelName)
        {
            var template = await ReadTemplateAsync("ServiceTemplate.txt");
            return template.Replace("{{ModelName}}", modelName);
        }

        public async Task<string> GenerateControllerCode(string modelName)
        {
            var template = await ReadTemplateAsync("ControllerTemplate.txt");
            return template.Replace("{{ModelName}}", modelName);
        }

        public async Task<string> GenerateRepositoryCode(string modelName)
        {
            var template = await ReadTemplateAsync("RepositoryTemplate.txt");
            return template
                .Replace("{{ModelName}}", modelName)
                .Replace("{{ModelNamePlural}}", Pluralize(modelName));
        }

        public async Task<string> GenerateDbContextCode(List<string> modelNames)
        {
            var template = await ReadTemplateAsync("DbContextTemplate.txt");

            var dbSets = string.Join(Environment.NewLine,
                modelNames.Select(name => $"        public DbSet<{name}> {Pluralize(name)} {{ get; set; }}"));

            return template.Replace("{{DbSets}}", dbSets);
        }


        // Generate the JwtAuthenticationManager
        public async Task<string> GenerateJwtAuthenticationManagerCode()
        {
            var template = await ReadTemplateAsync("JwtAuthenticationManagerTemplate.txt");
            return template;
        }

        // Generate the JwtMiddleware for token validation
        public async Task<string> GenerateJwtMiddlewareCode()
        {
            var template = await ReadTemplateAsync("JwtMiddlewareTemplate.txt");
            return template;
        }

        // Generate authorization code (role-based)
        public async Task<string> GenerateAuthorizationCode(List<string> roles)
        {
            var template = await ReadTemplateAsync("AuthorizationTemplate.txt");

            var roleChecks = string.Join(Environment.NewLine,
                roles.Select(role => $"        services.AddAuthorization(options => options.AddPolicy(\"{role}\", policy => policy.RequireRole(\"{role}\")));"));

            return template.Replace("{{RoleChecks}}", roleChecks);
        }

        private async Task<string> ReadTemplateAsync(string fileName)
        {
            var filePath = Path.Combine(_templateDirectory, fileName);
            if (!File.Exists(filePath))
                throw new FileNotFoundException($"Template file '{fileName}' not found in '{_templateDirectory}'.");

            return await File.ReadAllTextAsync(filePath);
        }



        public async Task<string> GenerateProgramCs()
        {
            var template = await ReadTemplateAsync("ProgramTemplate.txt");
            return template;
        }

        public async Task<string> GenerateProjectFile(string projectName)
        {
            var template = await ReadTemplateAsync("ProjectFileTemplate.txt");
            return template.Replace("{{ProjectName}}", projectName);
        }

        public async Task<string> GenerateAppSettingsJson()
        {
            var template = await ReadTemplateAsync("AppSettingsTemplate.txt");
            return template;
        }



        private string Pluralize(string name)
        {
            // Basic pluralization (can be replaced with a more sophisticated library if needed).
            if (name.EndsWith("y"))
                return name.Substring(0, name.Length - 1) + "ies";

            return name + "s";
        }
    }
}
