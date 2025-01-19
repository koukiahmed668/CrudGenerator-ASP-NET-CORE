# CRUD Generator for ASP.NET Core

This project is a CRUD (Create, Read, Update, Delete) generator for ASP.NET Core applications. It automates the creation of a simple CRUD application, allowing you to either generate the project through a user interface or use the CLI locally.

## Features

- **Automated Project Generation**: Generates a complete ASP.NET Core CRUD application packaged as a ZIP file.
- **Entity Framework Integration**: Utilizes Entity Framework Core for data access and management.
- **User Interface**: Generate CRUD applications via a web interface.
- **CLI Support**: Install and use the tool locally via the .NET CLI.

## How to Use

### Option 1: Generate CRUD Application via User Interface

1. Visit the website:  
   [CRUD Generator Web Interface](https://crudgenerator-asp-net-core.onrender.com/home)

2. Enter the details of the entity for which you want to generate CRUD operations.

3. Click the **Generate** button to download a ZIP file containing the generated ASP.NET Core CRUD application.

4. **Extract the ZIP File**: Once the ZIP file is downloaded, extract it to your preferred directory.

5. **Restore Dependencies**: In the project folder, run the following command to restore dependencies:

   ```bash
   dotnet restore
   ```
6. **Update Database**: Ensure your database connection string is correctly configured in the appsettings.json file. Then, apply any pending migrations:

```bash
   dotnet ef database update
   ```
7. **Run the Application**:
```bash
dotnet run
   ```
### Option 2: Use the CLI Locally

If you prefer to work with the CLI, you can install the CRUD generator tool and use it directly in your command line interface.

1. Install the CLI Tool:
Run the following command to install the tool globally on your machine:
```bash
dotnet tool install -g CrudGeneratorCli
```
2. Generate CRUD Application Using the CLI:
Once installed, you can use the tool by running:
```bash
crudgen
```

This will guide you through the process of generating a CRUD application, and the generated project will be saved in your current directory.


## Contributing

Contributions are welcome! Feel free to open issues or submit pull requests to enhance the functionality of this CRUD generator.
