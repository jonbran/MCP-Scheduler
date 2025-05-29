using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using McpScheduler.Core.Interfaces;
using McpScheduler.Core.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;

namespace McpScheduler.Api.Controllers
{
    /// <summary>
    /// API controller for managing conversations
    /// </summary>
    [ApiController]
    [Route("api/[controller]")]
    [Authorize]
    public class ConversationsController : ControllerBase
    {
        private readonly IConversationService _conversationService;
        private readonly ILogger<ConversationsController> _logger;

        /// <summary>
        /// Initializes a new instance of the <see cref="ConversationsController"/> class
        /// </summary>
        /// <param name="conversationService">The conversation service</param>
        /// <param name="logger">The logger</param>
        public ConversationsController(
            IConversationService conversationService,
            ILogger<ConversationsController> logger)
        {
            _conversationService = conversationService ?? throw new ArgumentNullException(nameof(conversationService));
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        }

        /// <summary>
        /// Creates a new scheduled conversation
        /// </summary>
        /// <param name="request">The conversation create request</param>
        /// <returns>The created conversation</returns>
        [HttpPost]
        [ProducesResponseType(StatusCodes.Status201Created)]
        [ProducesResponseType(StatusCodes.Status400BadRequest)]
        [ProducesResponseType(StatusCodes.Status500InternalServerError)]
        public async Task<ActionResult<Conversation>> CreateConversation(ConversationCreateRequest request)
        {
            try
            {
                var conversation = new Conversation
                {
                    ConversationText = request.ConversationText,
                    ScheduledTime = request.ScheduledTime,
                    Target = request.Target
                };

                var result = await _conversationService.ScheduleConversationAsync(conversation);

                _logger.LogInformation("Created conversation with ID {ConversationId}", result.Id);

                return CreatedAtAction(
                    nameof(GetConversation),
                    new { id = result.Id },
                    result);
            }
            catch (ArgumentException ex)
            {
                _logger.LogWarning(ex, "Invalid request received");
                return BadRequest(new { error = ex.Message });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error creating conversation");
                return StatusCode(500, new { error = "An error occurred while creating the conversation" });
            }
        }

        /// <summary>
        /// Gets a conversation by ID
        /// </summary>
        /// <param name="id">The conversation ID</param>
        /// <returns>The conversation if found</returns>
        [HttpGet("{id}")]
        [ProducesResponseType(StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status404NotFound)]
        [ProducesResponseType(StatusCodes.Status500InternalServerError)]
        public async Task<ActionResult<Conversation>> GetConversation(Guid id)
        {
            try
            {
                var result = await _conversationService.GetConversationAsync(id);
                if (result == null)
                {
                    _logger.LogWarning("Conversation with ID {ConversationId} not found", id);
                    return NotFound();
                }

                return Ok(result);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting conversation with ID {ConversationId}", id);
                return StatusCode(500, new { error = "An error occurred while retrieving the conversation" });
            }
        }

        /// <summary>
        /// Cancels a scheduled conversation
        /// </summary>
        /// <param name="id">The conversation ID</param>
        /// <returns>204 No Content if successful</returns>
        [HttpDelete("{id}")]
        [ProducesResponseType(StatusCodes.Status204NoContent)]
        [ProducesResponseType(StatusCodes.Status404NotFound)]
        [ProducesResponseType(StatusCodes.Status500InternalServerError)]
        public async Task<IActionResult> CancelConversation(Guid id)
        {
            try
            {
                var result = await _conversationService.CancelConversationAsync(id);
                if (!result)
                {
                    _logger.LogWarning("Conversation with ID {ConversationId} not found or could not be cancelled", id);
                    return NotFound();
                }

                _logger.LogInformation("Conversation with ID {ConversationId} cancelled successfully", id);
                return NoContent();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error cancelling conversation with ID {ConversationId}", id);
                return StatusCode(500, new { error = "An error occurred while cancelling the conversation" });
            }
        }

        /// <summary>
        /// Gets all conversations with optional pagination
        /// </summary>
        /// <param name="page">The page number (default: 1)</param>
        /// <param name="pageSize">The page size (default: 10, max: 100)</param>
        /// <param name="status">Optional status filter</param>
        /// <returns>The list of conversations</returns>
        [HttpGet]
        [ProducesResponseType(StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status500InternalServerError)]
        public async Task<ActionResult<ConversationsPagedResponse>> GetConversations(
            [FromQuery] int page = 1,
            [FromQuery] int pageSize = 10,
            [FromQuery] string? status = null)
        {
            try
            {
                // Validate pagination parameters
                if (page < 1) page = 1;
                if (pageSize < 1) pageSize = 10;
                if (pageSize > 100) pageSize = 100;

                var conversations = await _conversationService.GetConversationsAsync(page, pageSize, status);
                var totalCount = await _conversationService.GetConversationsCountAsync(status);

                var response = new ConversationsPagedResponse
                {
                    Data = conversations,
                    Page = page,
                    PageSize = pageSize,
                    TotalCount = totalCount,
                    TotalPages = (int)Math.Ceiling((double)totalCount / pageSize)
                };

                return Ok(response);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting conversations");
                return StatusCode(500, new { error = "An error occurred while retrieving conversations" });
            }
        }
    }

    /// <summary>
    /// Request model for creating a conversation
    /// </summary>
    public class ConversationCreateRequest
    {
        /// <summary>
        /// The text content of the conversation to be sent
        /// </summary>
        public required string ConversationText { get; set; }

        /// <summary>
        /// The time when the conversation should be initiated
        /// </summary>
        public DateTime ScheduledTime { get; set; }

        /// <summary>
        /// The target where the conversation should be sent
        /// </summary>
        public required ResponseTarget Target { get; set; }
    }

    /// <summary>
    /// Response model for paginated conversations
    /// </summary>
    public class ConversationsPagedResponse
    {
        /// <summary>
        /// The list of conversations
        /// </summary>
        public required IEnumerable<Conversation> Data { get; set; }

        /// <summary>
        /// The current page number
        /// </summary>
        public int Page { get; set; }

        /// <summary>
        /// The page size
        /// </summary>
        public int PageSize { get; set; }

        /// <summary>
        /// The total count of conversations
        /// </summary>
        public int TotalCount { get; set; }

        /// <summary>
        /// The total number of pages
        /// </summary>
        public int TotalPages { get; set; }
    }
}
