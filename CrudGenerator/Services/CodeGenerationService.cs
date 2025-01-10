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

        public async Task<string> GenerateModelCode(string modelName, List<(string Name, string Type)> attributes, List<(string RelatedModel, string RelationshipType)> relationships)
        {
            var template = await ReadTemplateAsync("ModelTemplate.txt");

            // Generate attributes for the model
            var attributeDefinitions = string.Join(Environment.NewLine,
                attributes.Select(attr => $"        public {attr.Type} {attr.Name} {{ get; set; }}"));

            // Generate navigation properties for relationships
            var navigationProperties = GenerateNavigationProperties(relationships);

            return template.Replace("{{ModelName}}", modelName)
                           .Replace("{{Attributes}}", attributeDefinitions)
                           .Replace("{{NavigationProperties}}", navigationProperties);
        }

        private string GenerateNavigationProperties(List<(string RelatedModel, string RelationshipType)> relationships)
        {
            var navigationProperties = new List<string>();

            foreach (var relationship in relationships)
            {
                if (relationship.RelationshipType == "OneToMany")
                {
                    navigationProperties.Add($"        public ICollection<{relationship.RelatedModel}> {relationship.RelatedModel}s {{ get; set; }}");
                }
                else if (relationship.RelationshipType == "ManyToOne")
                {
                    navigationProperties.Add($"        public int {relationship.RelatedModel}Id {{ get; set; }}  // Foreign Key");
                    navigationProperties.Add($"        public {relationship.RelatedModel} {relationship.RelatedModel} {{ get; set; }}");
                }
                else if (relationship.RelationshipType == "ManyToMany")
                {
                    // Handle many-to-many (using a junction table or a collection of models)
                    navigationProperties.Add($"        public ICollection<{relationship.RelatedModel}> {relationship.RelatedModel}s {{ get; set; }}");
                }
            }

            return string.Join(Environment.NewLine, navigationProperties);
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

        public async Task<string> GenerateDbContextCode(List<(string ModelName, List<(string RelatedModel, string RelationshipType)> Relationships)> models)
        {
            var template = await ReadTemplateAsync("DbContextTemplate.txt");

            var dbSets = string.Join(Environment.NewLine,
                models.Select(model => $"        public DbSet<{model.ModelName}> {Pluralize(model.ModelName)} {{ get; set; }}"));

            var relationships = GenerateRelationships(models);

            return template.Replace("{{DbSets}}", dbSets)
                           .Replace("{{Relationships}}", relationships);
        }

        private string GenerateRelationships(List<(string ModelName, List<(string RelatedModel, string RelationshipType)> Relationships)> models)
        {
            var relationships = new List<string>();

            foreach (var model in models)
            {
                foreach (var relationship in model.Relationships)
                {
                    if (relationship.RelationshipType == "OneToMany")
                    {
                        relationships.Add($"            modelBuilder.Entity<{model.ModelName}>().HasMany(m => m.{Pluralize(relationship.RelatedModel)}).WithOne().OnDelete(DeleteBehavior.Cascade);");
                    }
                    else if (relationship.RelationshipType == "ManyToOne")
                    {
                        relationships.Add($"            modelBuilder.Entity<{model.ModelName}>().HasOne(m => m.{relationship.RelatedModel}).WithMany().HasForeignKey(m => m.{relationship.RelatedModel}Id).OnDelete(DeleteBehavior.Cascade);");
                    }
                    else if (relationship.RelationshipType == "ManyToMany")
                    {
                        // Handle many-to-many relationships (using a join entity)
                        relationships.Add($"            modelBuilder.Entity<{model.ModelName}>().HasMany(m => m.{relationship.RelatedModel}s).WithMany().UsingEntity<JoinTable>();");
                    }
                }
            }

            return string.Join(Environment.NewLine, relationships);
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



        public async Task<string> GenerateProgramCs(List<string> modelNames)
        {
            var template = await ReadTemplateAsync("ProgramTemplate.txt");

            // Generate service registrations dynamically for each model
            var serviceRegistrations = string.Join(Environment.NewLine,
                modelNames.Select(name =>
                    $"builder.Services.AddScoped<I{name}Service, {name}Service>();"));

            // Generate repository registrations dynamically for each model
            var repositoryRegistrations = string.Join(Environment.NewLine,
                modelNames.Select(name =>
                    $"builder.Services.AddScoped<I{name}Repository, {name}Repository>();"));

            // Replace placeholders in the template with the generated registrations
            return template.Replace("{{ServiceRegistrations}}", serviceRegistrations)
                           .Replace("{{RepositoryRegistrations}}", repositoryRegistrations);
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
