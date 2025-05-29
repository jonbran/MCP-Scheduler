using System;
using System.Threading.Tasks;
using McpScheduler.Core.Interfaces;
using McpScheduler.Core.Models;
using Microsoft.Extensions.Logging;
using ModelContextProtocol.Server;
using System.ComponentModel;

namespace McpScheduler.Api.Services
{
    /// <summary>
    /// MCP tool implementation for scheduling conversations
    /// </summary>
    public class McpSchedulerToolService
    {
        private readonly IConversationService _conversationService;
        private readonly ILogger<McpSchedulerToolService> _logger;

        /// <summary>
        /// Initializes a new instance of the <see cref="McpSchedulerToolService"/> class
        /// </summary>
        /// <param name="conversationService">The conversation service</param>
        /// <param name="logger">The logger</param>
        public McpSchedulerToolService(
            IConversationService conversationService,
            ILogger<McpSchedulerToolService> logger)
        {
            _conversationService = conversationService ?? throw new ArgumentNullException(nameof(conversationService));
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        }

        /// <summary>
        /// Schedules a conversation for future delivery
        /// </summary>
        /// <param name="conversationText">The text content to be sent</param>
        /// <param name="scheduledTime">The time when the conversation should be delivered (ISO 8601 format)</param>
        /// <param name="endpoint">The endpoint where the conversation should be delivered</param>
        /// <param name="method">The HTTP method for the delivery (GET, POST, PUT, etc.)</param>
        /// <param name="additionalInfo">Additional information about the conversation</param>
        /// <returns>The ID of the scheduled conversation</returns>
        public async Task<string> ScheduleConversation(
            string conversationText,
            string scheduledTime,
            string endpoint,
            string method = "POST",
            string? additionalInfo = null)
        {
            try
            {
                _logger.LogInformation("MCP Tool received request to schedule conversation to {Endpoint} at {ScheduledTime}",
                    endpoint, scheduledTime);

                if (string.IsNullOrEmpty(conversationText))
                    throw new ArgumentException("Conversation text is required", nameof(conversationText));

                if (string.IsNullOrEmpty(scheduledTime))
                    throw new ArgumentException("Scheduled time is required", nameof(scheduledTime));

                if (string.IsNullOrEmpty(endpoint))
                    throw new ArgumentException("Endpoint is required", nameof(endpoint));

                if (!DateTime.TryParse(scheduledTime, out var parsedScheduledTime))
                    throw new ArgumentException("Invalid scheduled time format. Use ISO 8601 format.", nameof(scheduledTime));

                if (parsedScheduledTime <= DateTime.UtcNow)
                    throw new ArgumentException("Scheduled time must be in the future", nameof(scheduledTime));

                // Create conversation object using the internal conversation service
                var conversation = new Conversation
                {
                    ConversationText = conversationText,
                    ScheduledTime = parsedScheduledTime,
                    Target = new ResponseTarget
                    {
                        Endpoint = endpoint,
                        Method = method,
                        HeadersJson = System.Text.Json.JsonSerializer.Serialize(new Dictionary<string, string>
                        {
                            ["Content-Type"] = "application/json"
                        }),
                        AdditionalInfo = additionalInfo
                    }
                };

                var result = await _conversationService.ScheduleConversationAsync(conversation);

                _logger.LogInformation("MCP Tool successfully scheduled conversation with ID {ConversationId}", result.Id);
                return result.Id.ToString();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error in MCP Tool while scheduling conversation");
                throw;
            }
        }

        /// <summary>
        /// Gets the status of a scheduled conversation
        /// </summary>
        /// <param name="conversationId">The ID of the conversation</param>
        /// <returns>The status of the conversation</returns>
        public async Task<string> GetConversationStatus(string conversationId)
        {
            try
            {
                _logger.LogInformation("MCP Tool received request to get status of conversation {ConversationId}", conversationId);

                if (string.IsNullOrEmpty(conversationId))
                    throw new ArgumentException("Conversation ID is required", nameof(conversationId));

                if (!Guid.TryParse(conversationId, out var id))
                    throw new ArgumentException("Invalid conversation ID format", nameof(conversationId));

                var conversation = await _conversationService.GetConversationAsync(id);

                if (conversation == null)
                {
                    _logger.LogWarning("MCP Tool could not find conversation {ConversationId}", conversationId);
                    throw new ArgumentException("Conversation not found", nameof(conversationId));
                }

                var status = conversation.Status.ToString();
                _logger.LogInformation("MCP Tool retrieved status {Status} for conversation {ConversationId}",
                    status, conversationId);

                return status;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error in MCP Tool while getting conversation status for {ConversationId}", conversationId);
                throw;
            }
        }

        /// <summary>
        /// Cancels a scheduled conversation
        /// </summary>
        /// <param name="conversationId">The ID of the conversation to cancel</param>
        /// <returns>True if cancelled successfully, false otherwise</returns>
        public async Task<bool> CancelConversation(string conversationId)
        {
            try
            {
                _logger.LogInformation("MCP Tool received request to cancel conversation {ConversationId}", conversationId);

                if (string.IsNullOrEmpty(conversationId))
                    throw new ArgumentException("Conversation ID is required", nameof(conversationId));

                if (!Guid.TryParse(conversationId, out var id))
                    throw new ArgumentException("Invalid conversation ID format", nameof(conversationId));

                var result = await _conversationService.CancelConversationAsync(id);

                if (result)
                {
                    _logger.LogInformation("MCP Tool successfully cancelled conversation {ConversationId}", conversationId);
                }
                else
                {
                    _logger.LogWarning("MCP Tool could not cancel conversation {ConversationId} - not found or already completed", conversationId);
                }

                return result;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error in MCP Tool while cancelling conversation {ConversationId}", conversationId);
                throw;
            }
        }
    }

    /// <summary>
    /// Static MCP tools for Model Context Protocol integration
    /// </summary>
    [McpServerToolType]
    public static class McpSchedulerTools
    {
        [McpServerTool, Description("Schedules a conversation for future delivery.")]
        public static string ScheduleConversation(
            [Description("The text content to be sent")] string conversationText,
            [Description("The time when the conversation should be delivered (ISO 8601 format)")] string scheduledTime,
            [Description("The endpoint where the conversation should be delivered")] string endpoint,
            [Description("The HTTP method for the delivery (GET, POST, PUT, etc.)")] string method = "POST",
            [Description("Additional information about the conversation")] string? additionalInfo = null)
        {
            // Note: This is a static tool implementation that would need to be wired up to the actual service
            // For now, it returns a placeholder. In a full implementation, this would delegate to McpSchedulerToolService
            return Guid.NewGuid().ToString();
        }

        [McpServerTool, Description("Gets the status of a scheduled conversation.")]
        public static string GetConversationStatus(
            [Description("The ID of the conversation")] string conversationId)
        {
            // Note: This is a static tool implementation that would need to be wired up to the actual service
            return "Scheduled";
        }

        [McpServerTool, Description("Cancels a scheduled conversation.")]
        public static bool CancelConversation(
            [Description("The ID of the conversation to cancel")] string conversationId)
        {
            // Note: This is a static tool implementation that would need to be wired up to the actual service
            return true;
        }
    }
}
