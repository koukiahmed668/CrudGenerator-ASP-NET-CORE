﻿using CrudGenerator.Shared;
using CrudGenerator.Handlers;
using CrudGenerator.Interfaces;
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

        private readonly IEnumerable<ICodeGenerationHandler> _handlers;

        public CodeGenerationService(IEnumerable<ICodeGenerationHandler> handlers)
        {
            _handlers = handlers;
        }



        public async Task<string> GenerateModelCode(string modelName, List<(string Name, string Type)> attributes, List<Shared.Relationship> relationships, string projectName)
        {
            var handler = _handlers.OfType<ModelCodeGenerationHandler>().FirstOrDefault();
            if (handler == null) throw new InvalidOperationException("Model handler not found.");

            return await handler.GenerateCodeAsync(modelName, projectName, attributes, relationships);
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





        public async Task<string> GenerateServiceCode(string modelName, string projectName)
        {
            var handler = _handlers.OfType<ServiceCodeGenerationHandler>().FirstOrDefault();
            if (handler == null) throw new InvalidOperationException("Service handler not found.");

            return await handler.GenerateCodeAsync(modelName, projectName);
        }

        public async Task<string> GenerateControllerCode(string modelName, string projectName)
        {
            var handler = _handlers.OfType<ControllerCodeGenerationHandler>().FirstOrDefault();
            if (handler == null) throw new InvalidOperationException("Controller handler not found.");

            return await handler.GenerateCodeAsync(modelName, projectName);
        }

        public async Task<string> GenerateRepositoryCode(string modelName, string projectName)
        {
            var handler = _handlers.OfType<RepositoryCodeGenerationHandler>().FirstOrDefault();
            if (handler == null) throw new InvalidOperationException("Repository handler not found.");

            return await handler.GenerateCodeAsync(modelName, projectName);
        }

        public async Task<string> GenerateDbContextCode(List<ModelDefinition> models, bool includeJwtAuthentication, string projectName)
        {
            var template = await ReadTemplateAsync("DbContextTemplate.txt");

            // Generate DbSet properties dynamically
            var dbSets = string.Join(Environment.NewLine,
                models.Select(model => $"        public DbSet<{model.Name}> {Pluralize(model.Name)} {{ get; set; }}"));

            // Add User DbSet if JWT authentication is enabled
            if (includeJwtAuthentication)
            {
                dbSets = $"        public DbSet<User> Users {{ get; set; }}\n" + dbSets;
            }

            // Generate relationship configurations
            var relationshipConfigurations = string.Join(Environment.NewLine,
                models.SelectMany(model => model.Relationships.Select(rel => GenerateRelationshipConfiguration(model.Name, rel))));

            // Remove `using YourNamespace;` if JWT is not included
            var namespaceImport = includeJwtAuthentication ? "using YourNamespace;" : string.Empty;

            // Replace placeholders in the template
            return template.Replace("{{DbSets}}", dbSets)
                           .Replace("{{RelationshipConfigurations}}", relationshipConfigurations)
                           .Replace("{{ProjectName}}", projectName) // Replace {{ProjectName}}
                           .Replace("using YourNamespace;", namespaceImport);
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
        public async Task<string> GenerateJwtAuthenticationManagerCode(string projectName)
        {
            var template = await ReadTemplateAsync("JwtAuthenticationManagerTemplate.txt");
            return template.Replace("{{ProjectName}}", projectName);
        }

        // Generate the JwtMiddleware for token validation
        public async Task<string> GenerateJwtMiddlewareCode(string projectName)
        {
            var template = await ReadTemplateAsync("JwtMiddlewareTemplate.txt");
            return template.Replace("{{ProjectName}}", projectName);
        }

        public async Task<string> GenerateJwtAuthenticationControllerCode(string projectName)
        {
            var template = await ReadTemplateAsync("JwtAuthenticationControllerTemplate.txt");

            // Customize as necessary; replace placeholders in your template
            return template.Replace("{{ProjectName}}", projectName);
        }

        public async Task<string> GenerateUserServiceCode(string projectName)
        {
            var template = await ReadTemplateAsync("userAuth.txt");
            return template.Replace("{{ProjectName}}", projectName);
        }

        public async Task<string> GenerateUserRepositoryCode(string projectName)
        {
            var template = await ReadTemplateAsync("UserRepositoryTemplate.txt");
            return template.Replace("{{ProjectName}}", projectName);
        }

        public async Task<string> GenerateUserEntityCode(string projectName)
        {
            var template = await ReadTemplateAsync("UserEntityTemplate.txt");
            return template.Replace("{{ProjectName}}", projectName);
        }

        // Generate authorization code (role-based)
        public async Task<string> GenerateAuthorizationCode(List<string> roles, string projectName)
        {
            var template = await ReadTemplateAsync("AuthorizationTemplate.txt");

            var roleChecks = string.Join(Environment.NewLine,
                roles.Select(role => $"        services.AddAuthorization(options => options.AddPolicy(\"{role}\", policy => policy.RequireRole(\"{role}\")));"));

            return template.Replace("{{RoleChecks}}", roleChecks)
                .Replace("{{ProjectName}}", projectName);
        }

        private async Task<string> ReadTemplateAsync(string fileName)
        {
            var filePath = Path.Combine(_templateDirectory, fileName);
            if (!File.Exists(filePath))
                throw new FileNotFoundException($"Template file '{fileName}' not found in '{_templateDirectory}'.");

            return await File.ReadAllTextAsync(filePath);
        }



        public async Task<string> GenerateProgramCs(List<string> modelNames, bool includeJwtAuthentication, string projectName)
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

            // Generate JWT authentication section to include in the builder section
            var jwtBuilderSection = includeJwtAuthentication
                ? @"
        builder.Services.AddAuthentication(""Bearer"")
            .AddJwtBearer(options =>
            {
                options.TokenValidationParameters = new Microsoft.IdentityModel.Tokens.TokenValidationParameters
                {
                    ValidateIssuerSigningKey = true,
                    IssuerSigningKey = new Microsoft.IdentityModel.Tokens.SymmetricSecurityKey(System.Text.Encoding.UTF8.GetBytes(""YourJWTSecretKey"")),
                    ValidateIssuer = false,
                    ValidateAudience = false
                };
            });
        builder.Services.AddAuthorization();
        var key = ""YourSecretKeyHere""; // Replace with a secure key
        builder.Services.AddSingleton(new JwtAuthenticationManager(key));"
                : "";

            // Generate JWT middleware section to include after app.Build()
            var jwtMiddlewareSection = includeJwtAuthentication
                ? @"
        app.UseAuthentication();
        app.UseAuthorization();
        app.UseMiddleware<JwtMiddleware>(key);"
                : "";

            // Replace `using YourNamespace;` with the dynamic project name
            var namespaceImport = includeJwtAuthentication ? $"using {projectName};" : string.Empty;

            // Replace placeholders in the template with the generated content
            return template.Replace("{{ServiceRegistrations}}", serviceRegistrations)
                           .Replace("{{RepositoryRegistrations}}", repositoryRegistrations)
                           .Replace("{{JwtAuthentication}}", jwtBuilderSection)
                           .Replace("{{JwtMiddleware}}", jwtMiddlewareSection)
                           .Replace("{{ProjectName}}", projectName);
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
