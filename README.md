# MCP Scheduled Conversation Service

A C# .NET Core service implementing the Model Context Protocol (MCP) for scheduling future conversations with AI agents.

## Features

- Schedule conversations to be delivered at specified future times
- Support for both MySQL and SQL Server databases
- JWT authentication for secure API access
- Comprehensive API for conversation management
- MCP-compliant interface for AI agent integration
- Hangfire for reliable job scheduling
- Health monitoring endpoints

## Prerequisites

- .NET 9.0 SDK or later
- SQL Server or MySQL database
- Visual Studio 2022, Visual Studio Code, or any compatible IDE

## Getting Started

### Database Configuration

Configure your database connection in `appsettings.json`:

```json
"ConnectionStrings": {
  "DefaultConnection": "Server=localhost;Database=McpSchedulerDb;User=sa;Password=YourStrongPassword;TrustServerCertificate=True;"
},
"Database": {
  "Provider": "SqlServer" // Or "mysql" for MySQL
}
```

### JWT Configuration

Configure JWT authentication in `appsettings.json`:

```json
"Jwt": {
  "Key": "YourSuperSecretKeyForMcpSchedulerToSignJwtTokens",
  "Issuer": "McpScheduler",
  "Audience": "McpSchedulerClients",
  "ExpiryMinutes": 120
},
"Auth": {
  "ApiKey": "McpScheduler_API_Key_For_Development_Purposes_Only"
}
```

### Running the Application

1. Clone the repository
2. Navigate to the project directory
3. Run the migrations to create the database:

```bash
dotnet ef database update --project McpScheduler.Infrastructure --startup-project McpScheduler.Api
```

4. Start the application:

```bash
dotnet run --project McpScheduler.Api
```

5. Access the API documentation at `http://localhost:5146/swagger`

## API Endpoints

### Authentication

- `POST /api/auth/token` - Get JWT token for API access
- `POST /api/auth/validate` - Validate a JWT token

### Conversations

- `GET /api/conversations/{id}` - Get a conversation by ID
- `POST /api/conversations` - Create a new scheduled conversation
- `DELETE /api/conversations/{id}` - Cancel a scheduled conversation

### MCP Integration

- `POST /mcp/tools` - Get available MCP tools via MCP protocol
- `POST /mcp/execute` - Execute an MCP tool via MCP protocol
- `GET /api/mcptools/tools` - Get available MCP tools via REST
- `POST /api/mcptools/execute` - Execute MCP tool via REST

### Health

- `GET /health` - Get API health status

## MCP Tools

### scheduleConversation

Schedule a new conversation to be delivered at a future time.

Parameters:

- `conversationText` (string): The text content of the conversation
- `scheduledTime` (string): The time when the conversation should be delivered (ISO 8601 format)
- `endpoint` (string): The endpoint where the conversation should be sent
- `method` (string): The HTTP method to use (default: "POST")
- `additionalInfo` (string): Additional information for the conversation (optional)

### getConversationStatus

Get the status of a scheduled conversation.

Parameters:

- `conversationId` (string): The ID of the conversation

### cancelConversation

Cancel a scheduled conversation.

Parameters:

- `conversationId` (string): The ID of the conversation to cancel

## Running Tests

Run the test suite with:

```bash
dotnet test
```

## Using the HTTP Client

A sample HTTP client is provided in `McpScheduler.Api.http` for testing the API endpoints. This file can be used with the REST Client extension in Visual Studio Code or similar tools.

## License

[MIT](LICENSE)

## More Information

For more information about the Model Context Protocol (MCP), visit [modelcontextprotocol.io](https://modelcontextprotocol.io).
