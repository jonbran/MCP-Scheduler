using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using McpScheduler.Api.Services;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;

namespace McpScheduler.Api.Controllers
{
    /// <summary>
    /// Controller for exposing MCP (Model Context Protocol) tools via HTTP endpoints
    /// </summary>
    [ApiController]
    [Route("api/mcptools")]
    [Authorize]
    public class McpToolsController : ControllerBase
    {
        private readonly McpSchedulerToolService _mcpToolService;
        private readonly ILogger<McpToolsController> _logger;

        /// <summary>
        /// Initializes a new instance of the <see cref="McpToolsController"/> class
        /// </summary>
        /// <param name="mcpToolService">The MCP tool service</param>
        /// <param name="logger">The logger</param>
        public McpToolsController(
            McpSchedulerToolService mcpToolService,
            ILogger<McpToolsController> logger)
        {
            _mcpToolService = mcpToolService ?? throw new ArgumentNullException(nameof(mcpToolService));
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));

            _logger.LogInformation("McpToolsController initialized");
        }

        /// <summary>
        /// Gets the list of available MCP tools
        /// </summary>
        /// <returns>List of available MCP tools</returns>
        [HttpGet("tools")]
        [ProducesResponseType(StatusCodes.Status200OK)]
        public ActionResult<object> GetAvailableTools()
        {
            _logger.LogInformation("GetAvailableTools endpoint called");
            var tools = new
            {
                tools = new object[]
                {
                    new
                    {
                        name = "scheduleConversation",
                        description = "Schedules a conversation for future delivery",
                        inputSchema = new
                        {
                            type = "object",
                            properties = new
                            {
                                conversationText = new { type = "string", description = "The text content to be sent" },
                                scheduledTime = new { type = "string", description = "The time when the conversation should be delivered (ISO 8601 format)" },
                                endpoint = new { type = "string", description = "The endpoint where the conversation should be delivered" },
                                method = new { type = "string", description = "The HTTP method for the delivery (GET, POST, PUT, etc.)", @default = "POST" },
                                additionalInfo = new { type = "string", description = "Additional information about the conversation" }
                            },
                            required = new[] { "conversationText", "scheduledTime", "endpoint" }
                        }
                    },
                    new
                    {
                        name = "getConversationStatus",
                        description = "Gets the status of a scheduled conversation",
                        inputSchema = new
                        {
                            type = "object",
                            properties = new
                            {
                                conversationId = new { type = "string", description = "The ID of the conversation" }
                            },
                            required = new[] { "conversationId" }
                        }
                    },
                    new
                    {
                        name = "cancelConversation",
                        description = "Cancels a scheduled conversation",
                        inputSchema = new
                        {
                            type = "object",
                            properties = new
                            {
                                conversationId = new { type = "string", description = "The ID of the conversation to cancel" }
                            },
                            required = new[] { "conversationId" }
                        }
                    }
                }
            };

            return Ok(tools);
        }

        /// <summary>
        /// Executes an MCP tool
        /// </summary>
        /// <param name="request">The tool execution request</param>
        /// <returns>The result of the tool execution</returns>
        [HttpPost("execute")]
        public async Task<ActionResult<object>> ExecuteTool([FromBody] McpToolExecutionRequest request)
        {
            try
            {
                _logger.LogInformation("Executing MCP tool {ToolName}", request.Name);

                object result = request.Name?.ToLowerInvariant() switch
                {
                    "scheduleconversation" => await ExecuteScheduleConversation(request.Arguments),
                    "getconversationstatus" => await ExecuteGetConversationStatus(request.Arguments),
                    "cancelconversation" => await ExecuteCancelConversation(request.Arguments),
                    _ => throw new ArgumentException($"Unknown tool: {request.Name}")
                };

                return Ok(new { result });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error executing MCP tool {ToolName}", request.Name);
                return BadRequest(new { error = ex.Message });
            }
        }

        private async Task<string> ExecuteScheduleConversation(Dictionary<string, object>? arguments)
        {
            if (arguments == null)
                throw new ArgumentException("Arguments are required for scheduleConversation");

            var conversationText = arguments.GetValueOrDefault("conversationText")?.ToString()
                ?? throw new ArgumentException("conversationText is required");

            var scheduledTime = arguments.GetValueOrDefault("scheduledTime")?.ToString()
                ?? throw new ArgumentException("scheduledTime is required");

            var endpoint = arguments.GetValueOrDefault("endpoint")?.ToString()
                ?? throw new ArgumentException("endpoint is required");

            var method = arguments.GetValueOrDefault("method")?.ToString() ?? "POST";
            var additionalInfo = arguments.GetValueOrDefault("additionalInfo")?.ToString();

            return await _mcpToolService.ScheduleConversation(
                conversationText,
                scheduledTime,
                endpoint,
                method,
                additionalInfo);
        }

        private async Task<string> ExecuteGetConversationStatus(Dictionary<string, object>? arguments)
        {
            if (arguments == null)
                throw new ArgumentException("Arguments are required for getConversationStatus");

            var conversationId = arguments.GetValueOrDefault("conversationId")?.ToString()
                ?? throw new ArgumentException("conversationId is required");

            return await _mcpToolService.GetConversationStatus(conversationId);
        }

        private async Task<bool> ExecuteCancelConversation(Dictionary<string, object>? arguments)
        {
            if (arguments == null)
                throw new ArgumentException("Arguments are required for cancelConversation");

            var conversationId = arguments.GetValueOrDefault("conversationId")?.ToString()
                ?? throw new ArgumentException("conversationId is required");

            return await _mcpToolService.CancelConversation(conversationId);
        }
    }

    /// <summary>
    /// Request model for MCP tool execution
    /// </summary>
    public class McpToolExecutionRequest
    {
        /// <summary>
        /// The name of the tool to execute
        /// </summary>
        public string? Name { get; set; }

        /// <summary>
        /// The arguments to pass to the tool
        /// </summary>
        public Dictionary<string, object>? Arguments { get; set; }
    }
}
