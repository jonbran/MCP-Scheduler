using System;
using System.IO;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Design;
using Microsoft.Extensions.Configuration;
using Pomelo.EntityFrameworkCore.MySql.Infrastructure;

namespace McpScheduler.Infrastructure.Data
{
    /// <summary>
    /// Factory for creating DbContext instances during design time (for migrations)
    /// </summary>
    public class ApplicationDbContextFactory : IDesignTimeDbContextFactory<ApplicationDbContext>
    {
        /// <inheritdoc/>
        public ApplicationDbContext CreateDbContext(string[] args)
        {
            // Look for appsettings.json in the API project directory
            var basePath = Path.Combine(Directory.GetCurrentDirectory(), "..", "McpScheduler.Api");
            if (!Directory.Exists(basePath))
            {
                basePath = Directory.GetCurrentDirectory();
            }

            var configuration = new ConfigurationBuilder()
                .SetBasePath(basePath)
                .AddJsonFile("appsettings.json", optional: false, reloadOnChange: true)
                .AddJsonFile($"appsettings.{Environment.GetEnvironmentVariable("ASPNETCORE_ENVIRONMENT") ?? "Development"}.json", optional: true)
                .AddEnvironmentVariables()
                .Build();

            var optionsBuilder = new DbContextOptionsBuilder<ApplicationDbContext>();
            var connectionString = configuration.GetConnectionString("DefaultConnection");

            if (string.IsNullOrEmpty(connectionString))
            {
                // Use a default connection string for migrations
                connectionString = "Server=localhost;Port=3307;Database=McpSchedulerDb;User=mcpuser;Password=McpScheduler123!;";
            }

            var dbProvider = configuration["Database:Provider"]?.ToLowerInvariant() ?? "mysql";

            if (dbProvider == "mysql")
            {
                // Use specific server version instead of auto-detect to avoid connection issues during migration
                var serverVersion = new MySqlServerVersion(new Version(11, 4, 0));
                optionsBuilder.UseMySql(connectionString, serverVersion);
            }
            else
            {
                // Default to SQL Server
                optionsBuilder.UseSqlServer(connectionString);
            }

            return new ApplicationDbContext(optionsBuilder.Options);
        }
    }
}
