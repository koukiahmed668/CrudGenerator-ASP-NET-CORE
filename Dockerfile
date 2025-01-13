# Use the .NET SDK image to build the application
FROM mcr.microsoft.com/dotnet/sdk:8.0 AS build
ARG BUILD_CONFIGURATION=Release

# Set the environment to Development and indicate running in Docker
ENV ASPNETCORE_ENVIRONMENT=Development
ENV DOTNET_RUNNING_IN_CONTAINER=true

# Set the working directory in the container
WORKDIR /app

# Copy the solution file into the container
COPY CrudGenerator.sln ./

# Copy the source directories into the container
COPY CrudGenerator/ ./CrudGenerator/
COPY CrudGenerator.Client/ ./CrudGenerator.Client/
COPY CrudGenerator.Shared/ ./CrudGenerator.Shared/

# Restore dependencies
RUN dotnet restore "CrudGenerator.sln"

# Build the entire solution
RUN dotnet build "CrudGenerator.sln" -c $BUILD_CONFIGURATION -o /app/build

# Publish the server application
FROM build AS publish
RUN dotnet publish "CrudGenerator/CrudGenerator.csproj" -c $BUILD_CONFIGURATION -o /app/publish /p:UseAppHost=false

# Use the .NET runtime image for the server
FROM mcr.microsoft.com/dotnet/aspnet:8.0 AS base
WORKDIR /app
EXPOSE 8080

# Copy the published server application from the build image
COPY --from=publish /app/publish .

# Define the entry point for the server
ENTRYPOINT ["dotnet", "CrudGenerator.dll"]

# Additional stage for the Blazor Client
FROM mcr.microsoft.com/dotnet/sdk:8.0 AS client-build
ARG BUILD_CONFIGURATION=Release

# Set the working directory for the client build
WORKDIR /app

# Copy the solution file and source directories for the client and shared projects
COPY CrudGenerator.sln ./
COPY CrudGenerator.Client/ ./CrudGenerator.Client/
COPY CrudGenerator.Shared/ ./CrudGenerator.Shared/

# Restore dependencies for the client project
RUN dotnet restore "CrudGenerator.Client/CrudGenerator.Client.csproj"

# Build and publish the Blazor WebAssembly application
RUN dotnet publish "CrudGenerator.Client/CrudGenerator.Client.csproj" -c $BUILD_CONFIGURATION -o /app/client-dist

# Final stage for combining client and server
FROM base AS final
WORKDIR /app

# Copy the server app files from the previous stage
COPY --from=publish /app/publish .

# Copy the client app files from the client build stage
COPY --from=client-build /app/client-dist ./wwwroot

# Define the entry point for the server
ENTRYPOINT ["dotnet", "CrudGenerator.dll"]
