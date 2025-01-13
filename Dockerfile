# Build the API and Blazor client
FROM mcr.microsoft.com/dotnet/sdk:8.0 AS build
WORKDIR /app

# Copy and restore dependencies for both API and Client projects
COPY CrudGenerator.sln ./
COPY CrudGenerator/ ./CrudGenerator/
COPY CrudGenerator.Client/ ./CrudGenerator.Client/
COPY CrudGenerator.Shared/ ./CrudGenerator.Shared/
RUN dotnet restore "CrudGenerator.sln"

# Build both the API and the Blazor client
RUN dotnet build "CrudGenerator.sln" -c Release -o /app/build

# Publish the API and the Blazor client
FROM build AS publish
RUN dotnet publish "CrudGenerator/CrudGenerator.csproj" -c Release -o /app/publish /p:UseAppHost=false
RUN dotnet publish "CrudGenerator.Client/CrudGenerator.Client.csproj" -c Release -o /app/client-publish /p:UseAppHost=false

# Final image to serve both Blazor and API
FROM mcr.microsoft.com/dotnet/aspnet:8.0 AS base
WORKDIR /app
EXPOSE 8080

# Copy the published API files
COPY --from=publish /app/publish . 

# Copy the Blazor client files from the published Blazor app to the wwwroot folder
COPY --from=publish /app/client-publish/wwwroot /app/wwwroot

# Entry point for the API
ENTRYPOINT ["dotnet", "CrudGenerator.dll"]
