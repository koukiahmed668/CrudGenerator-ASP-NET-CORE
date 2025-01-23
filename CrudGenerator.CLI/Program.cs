using System;
using System.Collections.Generic;
using System.Data;
using System.Diagnostics;
using System.IO;
using System.IO.Compression;
using System.Linq;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Text;
using System.Text.Json;
using System.Text.Json.Serialization;
using System.Threading.Tasks;
using Spectre.Console;

class Program
{
    static async Task<int> Main(string[] args)
    {
        AnsiConsole.Markup("[bold blue]Welcome to the CRUD Generator CLI[/] :rocket:\n");

        // Prompt for Project Name
        var projectName = AnsiConsole.Ask<string>("[bold yellow]Enter the project name:[/]").Trim();

        // Show banner for model generation
        AnsiConsole.Write(new Spectre.Console.Rule("[yellow]Model Generation[/]").RuleStyle("blue").Centered());
        var models = await PromptForModels();

        // Relationships
        AnsiConsole.Write(new Spectre.Console.Rule("[yellow]Relationships[/]").RuleStyle("blue").Centered());
        var relationships = await PromptForRelationships();

        // JWT and Roles
        AnsiConsole.Write(new Spectre.Console.Rule("[yellow]JWT Authentication[/]").RuleStyle("blue").Centered());
        var includeJwt = AnsiConsole.Confirm("[yellow]Do you want to include JWT authentication?[/]");

        AnsiConsole.Write(new Spectre.Console.Rule("[yellow]Roles-Based Authentication[/]").RuleStyle("blue").Centered());
        var includeRoles = includeJwt && AnsiConsole.Confirm("[yellow]Do you want to include roles-based authentication?[/]");

        // Create the CodeGenerationRequest
        var request = new CodeGenerationRequest
        {
            ProjectName = projectName,
            Models = models,
            ResponseType = "zip",
            Roles = includeRoles ? new List<string> { "Admin", "User" } : new List<string>(),
            IncludeJwtAuthentication = includeJwt
        };

        // Summary of choices
        AnsiConsole.Write(new Spectre.Console.Rule("[yellow]Summary of Your Choices[/]").RuleStyle("green").Centered());
        AnsiConsole.Markup($"[bold green]Project Name:[/] {projectName}\n");
        AnsiConsole.Markup($"[bold green]Models:[/] {string.Join(", ", models.Select(m => m.Name))}\n");
        AnsiConsole.Markup($"[bold green]JWT Authentication:[/] {(includeJwt ? "Enabled" : "Disabled")}\n");
        AnsiConsole.Markup($"[bold green]Roles Authentication:[/] {(includeRoles ? "Enabled" : "Disabled")}\n");

        if (AnsiConsole.Confirm("[bold yellow]Is this information correct?[/]"))
        {
            // Send the request to the API
            await SendRequestToApi("https://crudgenerator-asp-net-core.onrender.com/api/CodeGeneration/generate", request);

            // GitHub Repository
            AnsiConsole.Write(new Spectre.Console.Rule("[yellow]GitHub Repository[/]").RuleStyle("blue").Centered());
            await HandleGitHubRepoCreation(projectName);
        }
        else
        {
            AnsiConsole.Markup("[red]Aborting...[/]");
            return 1;
        }

        return 0;
    }

    static async Task<List<ModelDefinition>> PromptForModels()
    {
        var models = new List<ModelDefinition>();
        while (AnsiConsole.Confirm("[yellow]Do you want to add a new model?[/]"))
        {
            var modelName = AnsiConsole.Ask<string>("[yellow]Enter model name:[/]").Trim();

            // Collect attributes
            var attributes = new List<AttributeDefinition>();
            AnsiConsole.Write(new Spectre.Console.Rule("[yellow]Attributes[/]").RuleStyle("blue").Centered());
            while (AnsiConsole.Confirm("[yellow]Do you want to add an attribute?[/]"))
            {
                var attributeName = AnsiConsole.Ask<string>("[yellow]Enter attribute name:[/]").Trim();
                var attributeType = AnsiConsole.Prompt(
                    new SelectionPrompt<string>()
                        .Title("[yellow]Select attribute type:[/]")
                        .AddChoices("int", "string", "DateTime", "bool", "decimal", "double", "Guid", "Custom"));

                if (attributeType == "Custom")
                {
                    attributeType = AnsiConsole.Ask<string>("[yellow]Enter custom type name:[/]").Trim();
                }

                attributes.Add(new AttributeDefinition { Name = attributeName, Type = attributeType });
            }

            models.Add(new ModelDefinition { Name = modelName, Attributes = attributes });
        }

        return models;
    }

    static async Task<List<Relationship>> PromptForRelationships()
    {
        var relationships = new List<Relationship>();
        while (AnsiConsole.Confirm("[yellow]Do you want to add a relationship?[/]"))
        {
            var sourceModel = AnsiConsole.Ask<string>("[yellow]Enter the source model for the relationship:[/]").Trim();
            var targetModel = AnsiConsole.Ask<string>("[yellow]Enter the target model for the relationship:[/]").Trim();
            var relationshipType = AnsiConsole.Prompt(
                new SelectionPrompt<string>()
                    .Title("[yellow]Select relationship type:[/]")
                    .AddChoices("OneToMany", "ManyToOne", "ManyToMany"));

            relationships.Add(new Relationship
            {
                PropertyName = $"{sourceModel}s", // Pluralize source model by default
                TargetModel = targetModel,
                Type = Enum.Parse<RelationshipType>(relationshipType)
            });
        }

        return relationships;
    }

    static async Task SendRequestToApi(string apiUrl, CodeGenerationRequest request)
    {
        using var client = new HttpClient();
        try
        {
            var content = new StringContent(JsonSerializer.Serialize(request), Encoding.UTF8, "application/json");

            // Ensure only one dynamic display is active at a time
            await AnsiConsole.Status()
                .StartAsync("Sending request to the server...", async ctx =>
                {
                    ctx.Status("Connecting to server...");
                    ctx.Spinner(Spinner.Known.Dots);

                    var response = await client.PostAsync(apiUrl, content);

                    if (response.IsSuccessStatusCode)
                    {
                        ctx.Status("Downloading generated code...");
                        var zipFile = await response.Content.ReadAsByteArrayAsync();

                        // Save ZIP file to disk
                        var currentDirectory = Directory.GetCurrentDirectory();
                        var zipFilePath = Path.Combine(currentDirectory, "generated_code.zip");
                        await File.WriteAllBytesAsync(zipFilePath, zipFile);

                        // Extract the ZIP
                        ctx.Status("Extracting files...");
                        var extractPath = Path.Combine(currentDirectory, "generated_code");
                        if (!Directory.Exists(extractPath))
                        {
                            Directory.CreateDirectory(extractPath);
                        }
                        ZipFile.ExtractToDirectory(zipFilePath, extractPath);
                        File.Delete(zipFilePath);

                        AnsiConsole.Markup("[bold green]Code generation complete![/]\n");
                        AnsiConsole.Markup($"[bold green]Generated code extracted to:[/] {extractPath}\n");
                    }
                    else
                    {
                        ctx.Status("Error occurred!");
                        AnsiConsole.Markup($"[bold red]Request failed with status code:[/] {response.StatusCode}\n");
                    }
                });
        }
        catch (Exception ex)
        {
            AnsiConsole.Markup($"[bold red]Error:[/] {ex.Message}\n");
        }
    }






    static async Task HandleGitHubRepoCreation(string projectName)
    {
        AnsiConsole.Write(new Spectre.Console.Rule("[yellow]GitHub Repository[/]").RuleStyle("blue").Centered());

        if (!AnsiConsole.Confirm("[yellow]Do you want to create a GitHub repository for this project?[/]")) return;

        string accessToken = await GetGitHubToken();

        bool useProjectName = AnsiConsole.Confirm("[yellow]Do you want to use the project name as the repository name?[/]");
        string repoName = useProjectName
            ? projectName
            : await PromptForCustomRepoName();

        await CreateGitHubRepoAndPush(projectName, repoName, accessToken);
    }

    static async Task<string> GetGitHubToken()
    {
        string tokenFilePath = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.UserProfile), ".crudgen_token");
        string accessToken = null;

        if (File.Exists(tokenFilePath))
        {
            AnsiConsole.MarkupLine("[yellow]A stored GitHub token was found. Do you want to use it?[/]");
            if (AnsiConsole.Confirm("[yellow]Use the stored token?[/]"))
            {
                accessToken = await File.ReadAllTextAsync(tokenFilePath);
                AnsiConsole.MarkupLine("[green]Using stored GitHub token.[/]");
            }
            else
            {
                accessToken = await PromptForNewGitHubToken(tokenFilePath);
            }
        }
        else
        {
            accessToken = await PromptForNewGitHubToken(tokenFilePath);
        }

        return accessToken;
    }

    static async Task<string> PromptForNewGitHubToken(string tokenFilePath)
    {
        AnsiConsole.MarkupLine("[yellow]Enter your new GitHub Personal Access Token:[/]");
        string accessToken = Console.ReadLine()?.Trim();

        if (AnsiConsole.Confirm("[yellow]Do you want to save this token for future use?[/]"))
        {
            await File.WriteAllTextAsync(tokenFilePath, accessToken);
            AnsiConsole.MarkupLine("[green]New token saved securely.[/]");
        }

        return accessToken;
    }

    static async Task<string> PromptForCustomRepoName()
    {
        return AnsiConsole.Ask<string>("[yellow]Enter the name for the GitHub repository:[/]");
    }

    static async Task CreateGitHubRepoAndPush(string projectName, string repoName, string accessToken)
    {
        string apiUrl = "https://api.github.com/user/repos";
        string generatedCodePath = Path.Combine(Directory.GetCurrentDirectory(), "generated_code");

        using var client = new HttpClient();
        client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("token", accessToken);
        client.DefaultRequestHeaders.UserAgent.Add(new ProductInfoHeaderValue("CRUD-Generator", "1.0"));

        var repoPayload = new
        {
            name = repoName,
            @private = true
        };

        var response = await client.PostAsJsonAsync(apiUrl, repoPayload);
        if (response.IsSuccessStatusCode)
        {
            AnsiConsole.MarkupLine($"[green]Repository '{repoName}' created successfully on GitHub.[/]");

            // Extract the username from the GitHub token API
            var userResponse = await client.GetAsync("https://api.github.com/user");
            if (!userResponse.IsSuccessStatusCode)
            {
                AnsiConsole.MarkupLine($"[red]Failed to fetch GitHub user info: {userResponse.StatusCode}[/]");
                var errorDetails = await userResponse.Content.ReadAsStringAsync();
                AnsiConsole.MarkupLine($"[red]Error details: {errorDetails}[/]");
                return;
            }

            var userJson = await userResponse.Content.ReadAsStringAsync();
            var user = JsonSerializer.Deserialize<GitHubUser>(userJson);
            string username = user?.Login;

            if (string.IsNullOrEmpty(username))
            {
                AnsiConsole.MarkupLine("[red]Failed to determine the GitHub username.[/]");
                return;
            }

            string repoUrl = $"https://github.com/{username}/{repoName}.git";

            // Navigate to the generated code folder and initialize Git
            Directory.SetCurrentDirectory(generatedCodePath);
            AnsiConsole.MarkupLine($"[green]Current directory:[/] {Directory.GetCurrentDirectory()}");

            RunCommand("git init");
            RunCommand($"git remote add origin https://{accessToken}@github.com/{username}/{repoName}.git");
            RunCommand("git checkout -b main");
            RunCommand("git add .");
            RunCommand("git commit -m 'Initial commit'");
            RunCommand("git push -u origin main");

            AnsiConsole.MarkupLine($"[green]Project pushed to GitHub repository: {repoUrl}[/]");
        }
        else
        {
            AnsiConsole.MarkupLine($"[red]Failed to create repository: {response.StatusCode}[/]");
            var errorDetails = await response.Content.ReadAsStringAsync();
            AnsiConsole.MarkupLine($"[red]Error details: {errorDetails}[/]");
        }
    }


    static void RunCommand(string command)
    {
        var process = new Process
        {
            StartInfo = new ProcessStartInfo
            {
                FileName = "bash",
                Arguments = $"-c \"{command}\"",
                RedirectStandardOutput = true,
                RedirectStandardError = true,
                UseShellExecute = false,
                CreateNoWindow = true,
            }
        };

        process.Start();
        string output = process.StandardOutput.ReadToEnd();
        string error = process.StandardError.ReadToEnd();
        process.WaitForExit();

        if (!string.IsNullOrEmpty(output))
        {
            AnsiConsole.MarkupLine($"[green]{output}[/]");
        }

        if (!string.IsNullOrEmpty(error))
        {
            AnsiConsole.MarkupLine($"[red]{error}[/]");
        }

        if (process.ExitCode != 0)
        {
            AnsiConsole.MarkupLine($"[red]Command failed: {command}[/]");
            throw new Exception($"Command failed: {command} with exit code {process.ExitCode}");
        }
    }



    public class CodeGenerationRequest
    {
        public string ProjectName { get; set; } 
        public List<ModelDefinition> Models { get; set; }
        public string ResponseType { get; set; } // "zip" or "text"
        public List<string> Roles { get; set; }
        public bool IncludeJwtAuthentication { get; set; }


    }

    // Model definition for code generation
    public class ModelDefinition
    {
        public string Name { get; set; }
        public List<AttributeDefinition> Attributes { get; set; }
        public List<Relationship> Relationships { get; set; }

    }

    // Attribute definition
    public class AttributeDefinition
    {
        public string Name { get; set; }
        public string Type { get; set; }
    }

    public class Relationship
    {
        public string PropertyName { get; set; }  // e.g., "Orders" in Customer model for one-to-many relationship
        public string TargetModel { get; set; }    // e.g., "Order" for one-to-many relationship

        [JsonConverter(typeof(RelationshipTypeConverter))]
        public RelationshipType Type { get; set; }
    }

    public enum RelationshipType
    {
        OneToMany,
        ManyToOne,
        ManyToMany
    }


    public class RelationshipTypeConverter : JsonConverter<RelationshipType>
    {
        public override RelationshipType Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
        {
            if (reader.TokenType == JsonTokenType.String)
            {
                string value = reader.GetString();
                return value switch
                {
                    "OneToMany" => RelationshipType.OneToMany,
                    "ManyToOne" => RelationshipType.ManyToOne,
                    "ManyToMany" => RelationshipType.ManyToMany,
                    _ => throw new JsonException($"Invalid value for RelationshipType: {value}")
                };
            }

            if (reader.TokenType == JsonTokenType.Number)
            {
                int value = reader.GetInt32();
                return value switch
                {
                    1 => RelationshipType.OneToMany,
                    2 => RelationshipType.ManyToOne,
                    3 => RelationshipType.ManyToMany,
                    _ => throw new JsonException($"Invalid value for RelationshipType: {value}")
                };
            }

            throw new JsonException($"Unexpected token {reader.TokenType} when reading RelationshipType.");
        }

        public override void Write(Utf8JsonWriter writer, RelationshipType value, JsonSerializerOptions options)
        {
            string enumValue = value switch
            {
                RelationshipType.OneToMany => "OneToMany",
                RelationshipType.ManyToOne => "ManyToOne",
                RelationshipType.ManyToMany => "ManyToMany",
                _ => throw new JsonException($"Invalid value for RelationshipType: {value}")
            };
            writer.WriteStringValue(enumValue);
        }
    }

    public class GitHubUser
    {
        [JsonPropertyName("login")]
        public string Login { get; set; }
    }


}
