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

# Copy the entire source directory into the container
COPY CrudGenerator/ ./CrudGenerator/

# Restore dependencies (this will restore based on the solution file)
RUN dotnet restore "CrudGenerator.sln"

# Build the application
RUN dotnet build "CrudGenerator.sln" -c $BUILD_CONFIGURATION -o /app/build

# Publish the application
FROM build AS publish
RUN dotnet publish "CrudGenerator.sln" -c $BUILD_CONFIGURATION -o /app/publish /p:UseAppHost=false

# Use the .NET runtime image for the final container
FROM mcr.microsoft.com/dotnet/aspnet:8.0 AS base
WORKDIR /app
EXPOSE 8080

# Copy the published application from the build image
COPY --from=publish /app/publish .

# Define the entry point
ENTRYPOINT ["dotnet", "CrudGenerator.dll"]
