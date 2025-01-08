# CRUD Generator for ASP.NET Core

This project is a CRUD (Create, Read, Update, Delete) generator for ASP.NET Core applications. It automates the creation of a simple CRUD application by generating a ZIP file containing the necessary project files and structure.

## Features

- **Automated Project Generation**: Generates a complete ASP.NET Core CRUD application packaged as a ZIP file.
- **Entity Framework Integration**: Utilizes Entity Framework Core for data access and management.

## Prerequisites

- [.NET SDK](https://dotnet.microsoft.com/download) installed on your machine.
- A code editor like [Visual Studio](https://visualstudio.microsoft.com/) or [Visual Studio Code](https://code.visualstudio.com/).

## Getting Started

1. **Clone the Repository**:

   ```bash
   git clone https://github.com/koukiahmed668/CrudGenerator-ASP-NET-CORE.git
   ```

2. **Navigate to the Project Directory**:

   ```bash
   cd CrudGenerator-ASP-NET-CORE
   ```

3. **Build the Project**:

   ```bash
   dotnet build
   ```

4. **Run the Application**:

   ```bash
   dotnet run
   ```

5. **Generate a CRUD Application**:

   - Access the running application in your browser.
   - Follow the on-screen instructions to specify the entity details for which you want to generate CRUD operations.
   - Click the "Generate" button to download the ZIP file containing the generated ASP.NET Core CRUD application.

6. **Extract and Check the Generated Code**:

   - Extract the contents of the downloaded ZIP file.
   - Open the extracted project in your preferred code editor.
   - **Restore Dependencies**:

     ```bash
     dotnet restore
     ```

   - **Update Database**:

     Ensure your database connection string is correctly configured in the `appsettings.json` file. Then, apply any pending migrations:

     ```bash
     dotnet ef database update
     ```

   - **Run the Application**:

     ```bash
     dotnet run
     ```

   - Navigate to `https://localhost:5001` in your browser to see the generated CRUD application in action.

## Customization

After generating the CRUD application, you can customize it further by:

- **Adding Business Logic**: Implement additional business rules in the controllers or services.
- **Extending Models**: Add more properties or validation attributes to the generated models.

## Contributing

Contributions are welcome! Feel free to open issues or submit pull requests to enhance the functionality of this CRUD generator.

