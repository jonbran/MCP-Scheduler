using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using McpScheduler.Api.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;

namespace McpScheduler.Api.Controllers
{
    /// <summary>
    /// Model Context Protocol API controller for the scheduler service
    /// </summary>
    [ApiController]
    [Route("mcp")]
    [Authorize]
    public class McpController : ControllerBase
    {
        private readonly McpSchedulerToolService _mcpService;
        private readonly ILogger<McpController> _logger;

        /// <summary>
        /// Initializes a new instance of the <see cref="McpController"/> class
        /// </summary>
        /// <param name="mcpService">The MCP scheduler tool service</param>
        /// <param name="logger">The logger</param>
        public McpController(
            McpSchedulerToolService mcpService,
            ILogger<McpController> logger)
        {
            _mcpService = mcpService ?? throw new ArgumentNullException(nameof(mcpService));
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        }

        /// <summary>
        /// Gets the MCP tool schema
        /// </summary>
        /// <returns>The tool schema definition</returns>
        [HttpGet("tools")]
        [ProducesResponseType(StatusCodes.Status200OK)]
        public ActionResult<McpToolsResponse> GetTools()
        {
            _logger.LogInformation("MCP tools schema requested");

            var response = new McpToolsResponse
            {
                ToolChoices = new List<McpTool>
                {
                    new McpTool
                    {
                        ToolId = "scheduleConversation",
                        ToolName = "Schedule Conversation",
                        ToolDescription = "Schedules a conversation to be delivered at a future time",
                        ToolParameters = new List<McpParameter>
                        {
                            new McpParameter
                            {
                                Name = "conversationText",
                                Description = "The text content to be sent",
                                Type = "string",
                                Required = true
                            },
                            new McpParameter
                            {
                                Name = "scheduledTime",
                                Description = "The time when the conversation should be delivered (ISO 8601 format)",
                                Type = "string",
                                Required = true
                            },
                            new McpParameter
                            {
                                Name = "endpoint",
                                Description = "The endpoint where the conversation should be delivered",
                                Type = "string",
                                Required = true
                            },
                            new McpParameter
                            {
                                Name = "method",
                                Description = "The HTTP method for the delivery (GET, POST, PUT, etc.)",
                                Type = "string",
                                Required = false
                            },
                            new McpParameter
                            {
                                Name = "additionalInfo",
                                Description = "Additional information about the conversation",
                                Type = "string",
                                Required = false
                            }
                        }
                    },
                    new McpTool
                    {
                        ToolId = "getConversationStatus",
                        ToolName = "Get Conversation Status",
                        ToolDescription = "Gets the status of a scheduled conversation",
                        ToolParameters = new List<McpParameter>
                        {
                            new McpParameter
                            {
                                Name = "conversationId",
                                Description = "The ID of the conversation",
                                Type = "string",
                                Required = true
                            }
                        }
                    },
                    new McpTool
                    {
                        ToolId = "cancelConversation",
                        ToolName = "Cancel Conversation",
                        ToolDescription = "Cancels a scheduled conversation",
                        ToolParameters = new List<McpParameter>
                        {
                            new McpParameter
                            {
                                Name = "conversationId",
                                Description = "The ID of the conversation to cancel",
                                Type = "string",
                                Required = true
                            }
                        }
                    }
                }
            };

            return Ok(response);
        }

        /// <summary>
        /// Executes an MCP tool
        /// </summary>
        /// <param name="request">The tool execution request</param>
        /// <returns>The tool execution result</returns>
        [HttpPost("execute")]
        [ProducesResponseType(StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status400BadRequest)]
        [ProducesResponseType(StatusCodes.Status500InternalServerError)]
        public async Task<ActionResult<McpToolResponse>> ExecuteTool([FromBody] McpToolRequest request)
        {
            try
            {
                _logger.LogInformation("MCP tool execution requested for tool {ToolId}", request.ToolId);

                object? result = null;

                switch (request.ToolId)
                {
                    case "scheduleConversation":
                        if (!TryGetParameterValue<string>(request.ToolParameters, "conversationText", out var conversationText) ||
                            !TryGetParameterValue<string>(request.ToolParameters, "scheduledTime", out var scheduledTime) ||
                            !TryGetParameterValue<string>(request.ToolParameters, "endpoint", out var endpoint))
                        {
                            return BadRequest(new { error = "Missing required parameters" });
                        }

                        TryGetParameterValue<string>(request.ToolParameters, "method", out var method);
                        TryGetParameterValue<string>(request.ToolParameters, "additionalInfo", out var additionalInfo);

                        result = await _mcpService.ScheduleConversation(
                            conversationText,
                            scheduledTime,
                            endpoint,
                            method ?? "POST",
                            additionalInfo);
                        break;

                    case "getConversationStatus":
                        if (!TryGetParameterValue<string>(request.ToolParameters, "conversationId", out var statusConversationId))
                        {
                            return BadRequest(new { error = "Missing conversationId parameter" });
                        }

                        result = await _mcpService.GetConversationStatus(statusConversationId);
                        break;

                    case "cancelConversation":
                        if (!TryGetParameterValue<string>(request.ToolParameters, "conversationId", out var cancelConversationId))
                        {
                            return BadRequest(new { error = "Missing conversationId parameter" });
                        }

                        result = await _mcpService.CancelConversation(cancelConversationId);
                        break;

                    default:
                        return BadRequest(new { error = $"Unknown tool ID: {request.ToolId}" });
                }

                return Ok(new McpToolResponse
                {
                    ToolId = request.ToolId,
                    ToolResult = result
                });
            }
            catch (ArgumentException ex)
            {
                _logger.LogWarning(ex, "Invalid MCP tool request");
                return BadRequest(new { error = ex.Message });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error executing MCP tool {ToolId}", request.ToolId);
                return StatusCode(500, new { error = $"An error occurred while executing the tool: {ex.Message}" });
            }
        }

        private bool TryGetParameterValue<T>(IDictionary<string, object> parameters, string key, out T value)
        {
            value = default!;

            if (parameters == null || !parameters.ContainsKey(key))
            {
                return false;
            }

            try
            {
                value = (T)Convert.ChangeType(parameters[key], typeof(T));
                return true;
            }
            catch
            {
                return false;
            }
        }
    }

    /// <summary>
    /// Response model for MCP tools schema
    /// </summary>
    public class McpToolsResponse
    {
        /// <summary>
        /// List of available tools
        /// </summary>
        public required List<McpTool> ToolChoices { get; set; }
    }

    /// <summary>
    /// Model representing an MCP tool
    /// </summary>
    public class McpTool
    {
        /// <summary>
        /// The unique identifier of the tool
        /// </summary>
        public required string ToolId { get; set; }

        /// <summary>
        /// The display name of the tool
        /// </summary>
        public required string ToolName { get; set; }

        /// <summary>
        /// The description of the tool
        /// </summary>
        public required string ToolDescription { get; set; }

        /// <summary>
        /// The parameters of the tool
        /// </summary>
        public required List<McpParameter> ToolParameters { get; set; }
    }

    /// <summary>
    /// Model representing an MCP tool parameter
    /// </summary>
    public class McpParameter
    {
        /// <summary>
        /// The name of the parameter
        /// </summary>
        public required string Name { get; set; }

        /// <summary>
        /// The description of the parameter
        /// </summary>
        public required string Description { get; set; }

        /// <summary>
        /// The type of the parameter
        /// </summary>
        public required string Type { get; set; }

        /// <summary>
        /// Whether the parameter is required
        /// </summary>
        public bool Required { get; set; }
    }

    /// <summary>
    /// Request model for MCP tool execution
    /// </summary>
    public class McpToolRequest
    {
        /// <summary>
        /// The ID of the tool to execute
        /// </summary>
        public required string ToolId { get; set; }

        /// <summary>
        /// The parameters for the tool execution
        /// </summary>
        public required Dictionary<string, object> ToolParameters { get; set; }
    }

    /// <summary>
    /// Response model for MCP tool execution
    /// </summary>
    public class McpToolResponse
    {
        /// <summary>
        /// The ID of the executed tool
        /// </summary>
        public required string ToolId { get; set; }

        /// <summary>
        /// The result of the tool execution
        /// </summary>
        public required object ToolResult { get; set; }
    }
}
