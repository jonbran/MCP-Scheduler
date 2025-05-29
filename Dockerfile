# Use the official .NET 9.0 runtime as the base image
FROM mcr.microsoft.com/dotnet/aspnet:9.0 AS base
WORKDIR /app
EXPOSE 8080
EXPOSE 8081

# Use the .NET 9.0 SDK for building
FROM mcr.microsoft.com/dotnet/sdk:9.0 AS build
ARG BUILD_CONFIGURATION=Release
WORKDIR /src

# Copy project files
COPY ["McpScheduler.Api/McpScheduler.Api.csproj", "McpScheduler.Api/"]
COPY ["McpScheduler.Core/McpScheduler.Core.csproj", "McpScheduler.Core/"]
COPY ["McpScheduler.Infrastructure/McpScheduler.Infrastructure.csproj", "McpScheduler.Infrastructure/"]

# Restore dependencies
RUN dotnet restore "McpScheduler.Api/McpScheduler.Api.csproj"

# Copy all source code
COPY . .

# Build the application
WORKDIR "/src/McpScheduler.Api"
RUN dotnet build "McpScheduler.Api.csproj" -c $BUILD_CONFIGURATION -o /app/build

# Publish the application
FROM build AS publish
ARG BUILD_CONFIGURATION=Release
RUN dotnet publish "McpScheduler.Api.csproj" -c $BUILD_CONFIGURATION -o /app/publish /p:UseAppHost=false

# Final stage - runtime
FROM base AS final
WORKDIR /app
COPY --from=publish /app/publish .

# Install curl for health checks
RUN apt-get update && apt-get install -y curl && rm -rf /var/lib/apt/lists/*

# Create a non-root user for security
RUN adduser --disabled-password --home /app --gecos '' appuser && chown -R appuser /app
USER appuser

# Set environment variables
ENV ASPNETCORE_URLS=http://+:8080
ENV ASPNETCORE_ENVIRONMENT=Production

ENTRYPOINT ["dotnet", "McpScheduler.Api.dll"]
