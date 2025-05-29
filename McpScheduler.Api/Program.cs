using System;
using System.Linq;
using System.Text;
using McpScheduler.Api.Controllers;
using McpScheduler.Api.Services;
using McpScheduler.Core.Interfaces;
using McpScheduler.Infrastructure;
using McpScheduler.Infrastructure.Repositories;
using McpScheduler.Infrastructure.Services;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Diagnostics.HealthChecks;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Diagnostics.HealthChecks;
using Microsoft.IdentityModel.Tokens;
using Microsoft.OpenApi.Models;
using ModelContextProtocol.Server;
using Quartz;

var builder = WebApplication.CreateBuilder(args);

// Add controller services
builder.Services.AddControllers();
builder.Services.AddHttpClient();

// Add database context
var dbProvider = builder.Configuration["Database:Provider"]?.ToLowerInvariant();
var connectionString = builder.Configuration.GetConnectionString("DefaultConnection");

if (string.IsNullOrEmpty(connectionString))
{
    throw new InvalidOperationException("Connection string 'DefaultConnection' not found.");
}

if (dbProvider == "mysql")
{
    builder.Services.AddDbContext<ApplicationDbContext>(options =>
        options.UseMySql(connectionString, ServerVersion.AutoDetect(connectionString)));
}
else
{
    // Default to SQL Server
    builder.Services.AddDbContext<ApplicationDbContext>(options =>
        options.UseSqlServer(connectionString));
}

// Add services
builder.Services.AddScoped<IConversationRepository, ConversationRepository>();
builder.Services.AddScoped<IHttpClientService, HttpClientService>();
builder.Services.AddScoped<ISchedulerService, QuartzSchedulerService>();
builder.Services.AddScoped<IConversationService, ConversationService>();
builder.Services.AddScoped<IJwtTokenService, JwtTokenService>();
builder.Services.AddScoped<McpSchedulerToolService>();

// Add Quartz services for dependency injection
builder.Services.AddQuartz(q =>
{
    // Add jobs to the DI container (DI job factory is enabled by default in Quartz 3.14+)
    q.AddJob<QuartzSchedulerService.ConversationExecutionJob>(opts => opts.WithIdentity("ConversationExecutionJob").StoreDurably());// Configure the job store for MySQL
    var connectionString = builder.Configuration.GetConnectionString("DefaultConnection")
        ?? throw new InvalidOperationException("Connection string 'DefaultConnection' not found.");
    q.UsePersistentStore(s =>
    {
        s.UseProperties = true;
        s.UseMySqlConnector(mysql =>
        {
            mysql.ConnectionString = connectionString;
            mysql.TablePrefix = "QRTZ_";
        });
        s.UseNewtonsoftJsonSerializer();
    });
});
builder.Services.AddQuartzHostedService(options =>
{
    // When shutting down we want jobs to complete gracefully
    options.WaitForJobsToComplete = true;
});

// Register the job class in DI
builder.Services.AddScoped<QuartzSchedulerService.ConversationExecutionJob>();

// Add JWT Authentication
builder.Services.AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
    .AddJwtBearer(options =>
    {
        options.TokenValidationParameters = new TokenValidationParameters
        {
            ValidateIssuer = true,
            ValidateAudience = false, // Temporarily disable for debugging
            ValidateLifetime = true,
            ValidateIssuerSigningKey = true,
            ValidIssuer = builder.Configuration["Jwt:Issuer"],
            ValidAudience = builder.Configuration["Jwt:Audience"],
            IssuerSigningKey = new SymmetricSecurityKey(
                Encoding.UTF8.GetBytes(builder.Configuration["Jwt:Key"] ?? throw new InvalidOperationException("JWT Key not configured"))),
            // Fix signature validation issues - remove RequireSignedTokens as it can cause kid validation issues
            RequireExpirationTime = true,
            ClockSkew = TimeSpan.FromMinutes(5)
        };
    });

// Configure health checks
builder.Services.AddHealthChecks()
    .AddDbContextCheck<ApplicationDbContext>()
    .AddCheck("self", () => HealthCheckResult.Healthy());

// Add Swagger with JWT authentication
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen(c =>
{
    c.SwaggerDoc("v1", new OpenApiInfo { Title = "MCP Scheduler API", Version = "v1" });

    // Configure Swagger to use JWT Authentication
    c.AddSecurityDefinition("Bearer", new OpenApiSecurityScheme
    {
        Description = "JWT Authorization header using the Bearer scheme. Example: \"Authorization: Bearer {token}\"",
        Name = "Authorization",
        In = ParameterLocation.Header,
        Type = SecuritySchemeType.ApiKey,
        Scheme = "Bearer"
    });

    c.AddSecurityRequirement(new OpenApiSecurityRequirement
    {
        {
            new OpenApiSecurityScheme
            {
                Reference = new OpenApiReference
                {
                    Type = ReferenceType.SecurityScheme,
                    Id = "Bearer"
                }
            },
            Array.Empty<string>()
        }
    });
});

// Add MCP server services - removed STDIO transport for HTTP compatibility
// Note: MCP tools will be exposed via HTTP endpoints instead

var app = builder.Build();

// Configure the HTTP request pipeline
if (app.Environment.IsDevelopment())
{
    app.UseDeveloperExceptionPage();
    app.UseSwagger();
    app.UseSwaggerUI(c => c.SwaggerEndpoint("/swagger/v1/swagger.json", "MCP Scheduler API v1"));
}

app.UseHttpsRedirection();

app.UseRouting();
app.UseAuthentication();
app.UseAuthorization();

// Add a welcome page with API information
app.MapGet("/", () => Results.Content(
    @"<!DOCTYPE html>
<html>
<head>
    <title>MCP Scheduler API</title>
    <style>
        body { font-family: Arial, sans-serif; max-width: 800px; margin: 0 auto; padding: 20px; }
        h1, h2, h3 { color: #333; }
        li { margin-bottom: 10px; }
        code { background-color: #f4f4f4; padding: 2px 5px; border-radius: 3px; }
        .endpoint { color: #0066cc; font-weight: bold; }
        .method { color: #009688; font-weight: bold; }
    </style>
</head>
<body>
    <h1>MCP Scheduled Conversation Service</h1>
    <p>A .NET service implementing the Model Context Protocol (MCP) for scheduling future conversations.</p>
    
    <h2>Available Endpoints:</h2>
    <ul>
        <li><span class='method'>GET</span> <span class='endpoint'>/health</span> - API health status</li>
        <li><span class='method'>POST</span> <span class='endpoint'>/api/auth/token</span> - Get JWT token</li>        <li><span class='method'>POST</span> <span class='endpoint'>/api/conversations</span> - Schedule a conversation</li>
        <li><span class='method'>GET</span> <span class='endpoint'>/api/conversations/{id}</span> - Get conversation status</li>
        <li><span class='method'>DELETE</span> <span class='endpoint'>/api/conversations/{id}</span> - Cancel a conversation</li>
        <li><span class='method'>GET</span> <span class='endpoint'>/api/mcptools/tools</span> - Get available MCP tools via REST</li>
        <li><span class='method'>POST</span> <span class='endpoint'>/api/mcptools/execute</span> - Execute MCP tool via REST</li>
        <li><span class='method'>POST</span> <span class='endpoint'>/mcp/tools</span> - Get available MCP tools</li>
        <li><span class='method'>POST</span> <span class='endpoint'>/mcp/execute</span> - Execute MCP tool</li>
    </ul>
    
    <h2>Documentation:</h2>
    <ul>
        <li><a href='/swagger'>API Documentation (Swagger)</a></li>
    </ul>
    
    <h3>MCP Implementation:</h3>
    <p>This service implements the Model Context Protocol (MCP) to allow AI agents to schedule conversations for future delivery.</p>
    <p>Available MCP tools:</p>
    <ul>
        <li><code>scheduleConversation</code> - Schedule a new conversation</li>
        <li><code>getConversationStatus</code> - Get status of a conversation</li>
        <li><code>cancelConversation</code> - Cancel a scheduled conversation</li>
    </ul>
</body>
</html>", "text/html"));

app.MapControllers();
app.MapHealthChecks("/health", new HealthCheckOptions
{
    ResponseWriter = HealthController.WriteResponse
});

// Ensure database is created and migrated on startup
using (var scope = app.Services.CreateScope())
{
    var services = scope.ServiceProvider;
    var logger = services.GetRequiredService<ILogger<Program>>();

    try
    {
        logger.LogInformation("Initializing database...");
        var dbContext = services.GetRequiredService<ApplicationDbContext>();

        // Apply migrations
        dbContext.Database.Migrate();
        logger.LogInformation("Database migrations applied successfully.");

        // Check if we need to seed initial data
        if (!dbContext.Conversations.Any())
        {
            logger.LogInformation("Seeding initial conversation data...");
            // Add seed data logic here if needed
        }
    }
    catch (Exception ex)
    {
        logger.LogError(ex, "An error occurred while initializing the database.");
        // In a production environment, you might want to continue application startup
        // even if database initialization fails, but that's a business decision
    }
}

// Run the MCP server
await app.RunAsync();
