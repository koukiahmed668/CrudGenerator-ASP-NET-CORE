# Build the API (CrudGenerator server-side project)
FROM mcr.microsoft.com/dotnet/sdk:8.0 AS build
WORKDIR /app

# Copy and restore dependencies for both API and Client projects
COPY CrudGenerator.sln ./ 
COPY CrudGenerator/ ./CrudGenerator/
COPY CrudGenerator.Client/ ./CrudGenerator.Client/
COPY CrudGenerator.Shared/ ./CrudGenerator.Shared/
RUN dotnet restore "CrudGenerator.sln"
RUN dotnet build "CrudGenerator.sln" -c Release -o /app/build

# Publish the API
FROM build AS publish
RUN dotnet publish "CrudGenerator/CrudGenerator.csproj" -c Release -o /app/publish /p:UseAppHost=false

# Final image to serve both Blazor and API
FROM mcr.microsoft.com/dotnet/aspnet:8.0 AS base
WORKDIR /app
EXPOSE 8080

# Copy the published API and Blazor client files
COPY --from=publish /app/publish .
COPY --from=build /app/CrudGenerator.Client/wwwroot /app/wwwroot  # Copy Blazor client files

# Entry point for the API
ENTRYPOINT ["dotnet", "CrudGenerator.dll"]
