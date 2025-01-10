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








        public async Task<string> GenerateModelCode(string modelName, List<(string Name, string Type)> attributes, List<Relationship> relationships)
        {
            var template = await ReadTemplateAsync("ModelTemplate.txt");

            // Generate basic attribute definitions
            var attributeDefinitions = string.Join(Environment.NewLine,
                attributes.Select(attr => $"        public {attr.Type} {attr.Name} {{ get; set; }}"));

            // Generate foreign key attributes for the "many" side of relationships
            var foreignKeyAttributes = string.Join(Environment.NewLine,
                relationships.Where(r => r.Type == RelationshipType.ManyToOne)
                              .Select(rel => $"        public int {rel.TargetModel}Id {{ get; set; }}"));

            // Merge the generated foreign key attributes with the existing attributes
            attributeDefinitions = string.Join(Environment.NewLine, new[] { attributeDefinitions, foreignKeyAttributes });

            // Generate the relationship code (navigation properties)
            var relationshipDefinitions = string.Join(Environment.NewLine,
                relationships.Select(rel => GenerateRelationshipCode(rel)));

            return template.Replace("{{ModelName}}", modelName)
                           .Replace("{{Attributes}}", attributeDefinitions)
                           .Replace("{{Relationships}}", relationshipDefinitions);
        }

        private string GenerateRelationshipCode(Relationship relationship)
        {
            switch (relationship.Type)
            {
                case RelationshipType.OneToMany:
                    // For a One-to-Many relationship, the "one" side has a collection (no FK here)
                    return $"        public ICollection<{relationship.TargetModel}> {relationship.PropertyName} {{ get; set; }}";

                case RelationshipType.ManyToOne:
                    // For a Many-to-One relationship, the "many" side has both FK and navigation property
                    return $"        public {relationship.TargetModel} {relationship.PropertyName} {{ get; set; }}";

                case RelationshipType.ManyToMany:
                    // For a Many-to-Many relationship, both sides have collections
                    return $"        public ICollection<{relationship.TargetModel}> {relationship.PropertyName} {{ get; set; }}";

                default:
                    return string.Empty;
            }
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

        public async Task<string> GenerateDbContextCode(List<ModelDefinition> models)
        {
            var template = await ReadTemplateAsync("DbContextTemplate.txt");

            // Generate DbSet properties
            var dbSets = string.Join(Environment.NewLine,
                models.Select(model => $"        public DbSet<{model.Name}> {Pluralize(model.Name)} {{ get; set; }}"));

            // Generate relationship configurations
            var relationshipConfigurations = string.Join(Environment.NewLine,
                models.SelectMany(model => model.Relationships.Select(rel => GenerateRelationshipConfiguration(model.Name, rel))));

            return template.Replace("{{DbSets}}", dbSets)
                           .Replace("{{RelationshipConfigurations}}", relationshipConfigurations);
        }

        private string GenerateRelationshipConfiguration(string modelName, Relationship relationship)
        {
            switch (relationship.Type)
            {
                case RelationshipType.OneToMany:
                    // Configure a one-to-many relationship
                    return $@"
            modelBuilder.Entity<{modelName}>()
                .HasMany(e => e.{relationship.PropertyName})
                .WithOne(e => e.{modelName})
                .HasForeignKey(e => e.{modelName}Id);";

                case RelationshipType.ManyToOne:
                    // Configure a many-to-one relationship
                    return $@"
            modelBuilder.Entity<{modelName}>()
                .HasOne(e => e.{relationship.PropertyName})
                .WithMany(e => e.{modelName})
                .HasForeignKey(e => e.{relationship.TargetModel}Id);";

                case RelationshipType.ManyToMany:
                    // Configure a many-to-many relationship using a join table
                    var joinTableName = $"{modelName}_{relationship.TargetModel}";
                    return $@"
            modelBuilder.Entity<{modelName}>()
                .HasMany(e => e.{relationship.PropertyName})
                .WithMany(e => e.{modelName})
                .UsingEntity(j => j.ToTable(""{joinTableName}""));";

                default:
                    return string.Empty;
            }
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
